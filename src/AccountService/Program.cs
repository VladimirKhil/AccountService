using AccountService.Configuration;
using AccountService.Contract.Requests;
using AccountService.Contract.Responses;
using AccountService.Contracts;
using AccountService.Database;
using AccountService.Middlewares;
using AccountService.Services;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Serilog;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
	.WriteTo.Console(new Serilog.Formatting.Display.MessageTemplateTextFormatter(
		"[{Timestamp:yyyy/MM/dd HH:mm:ss} {Level}] {Message:lj} {Exception}{NewLine}"))
	.ReadFrom.Configuration(ctx.Configuration));

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors("TauriCors");
app.UseMiddleware<AdminAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/v1/auth/steam", LoginBySteamAsync);
app.MapPost("/api/v1/admin/validate", ValidateSessionAsync);
app.MapGet("/api/v1/account/me", GetMeAsync).RequireAuthorization();
app.MapPatch("/api/v1/account/me", UpdateMeAsync).RequireAuthorization();
app.MapPost("/api/v1/account/me/delete", DeleteMeAsync).RequireAuthorization();

CreateDatabase(app);
ApplyMigrations(app);

app.Run();

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
	services.Configure<SteamOptions>(configuration.GetSection(SteamOptions.ConfigurationSectionName));
	services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.ConfigurationSectionName));
	services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.ConfigurationSectionName));

	services.AddAccountDatabase(configuration);
	ConfigureMigrationRunner(services, configuration);

	services.AddSingleton<ITokenService, TokenService>();
	services.AddScoped<IAccountManager, AccountManager>();
	services.AddHostedService<PurgeDeletedAccountsHostedService>();

	services.AddHttpClient<ISteamAuthorizationService, SteamAuthorizationService>((sp, client) =>
	{
		var options = sp.GetRequiredService<IOptions<SteamOptions>>().Value;
		client.BaseAddress = new Uri(options.BaseAddress);
		client.Timeout = TimeSpan.FromSeconds(15);
	});

	services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
		.AddJwtBearer();

	services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

	services.AddAuthorization();

    services.AddCors(options =>
    {
        options.AddPolicy("TauriCors", policy =>
            policy
                .WithOrigins(
                    "http://tauri.localhost",
                    "https://tauri.localhost")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });
}

static void ConfigureMigrationRunner(IServiceCollection services, IConfiguration configuration)
{
	var dbConnectionString = configuration.GetConnectionString("AccountService");

	services
		.AddFluentMigratorCore()
		.ConfigureRunner(migratorBuilder =>
			migratorBuilder
				.AddPostgres()
				.WithGlobalConnectionString(dbConnectionString)
				.ScanIn(typeof(DbConstants).Assembly).For.Migrations())
		.AddLogging(lb => lb.AddFluentMigratorConsole());
}

static void CreateDatabase(WebApplication app)
{
	var dbConnectionString = app.Configuration.GetConnectionString("AccountService");

	var connectionStringBuilder = new DbConnectionStringBuilder
	{
		ConnectionString = dbConnectionString,
	};

	connectionStringBuilder["Database"] = "postgres";

	DatabaseExtensions.EnsureExists(connectionStringBuilder.ConnectionString!, DbConstants.Schema);
}

static void ApplyMigrations(WebApplication app)
{
	using var scope = app.Services.CreateScope();
	var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

	if (runner.HasMigrationsToApplyUp())
	{
		runner.MigrateUp();
	}
}

static async Task<IResult> LoginBySteamAsync(
	SteamLoginRequest request,
	HttpContext context,
	IAccountManager accountManager,
	IOptions<JwtOptions> jwtOptions,
	CancellationToken cancellationToken)
{
	try
	{
		var auth = await accountManager.LoginBySteamAsync(request.AuthTicket, cancellationToken);
		var profile = await accountManager.GetByUserIdAsync(auth.UserId, cancellationToken);

		if (profile == null)
		{
			return Results.Problem("Authorized user account was not found", statusCode: 500);
		}

		var token = await accountManager.CreateSessionTokenAsync(profile.UserId, profile.Username, cancellationToken);

		var options = jwtOptions.Value;
		var expires = DateTimeOffset.UtcNow.AddMinutes(options.ExpiresMinutes);

		context.Response.Cookies.Append(options.CookieName, token, new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.None,
			Expires = expires,
			IsEssential = true,
			Path = "/",
		});

		return Results.Ok(auth);
	}
	catch (InvalidOperationException ex)
	{
		return Results.BadRequest(ex.Message);
	}
}

static async Task<IResult> ValidateSessionAsync(
	ValidateSessionRequest request,
	HttpContext context,
	IOptions<JwtOptions> jwtOptions,
	IAccountManager accountManager,
	CancellationToken cancellationToken)
{
	var token = request.Token;

	if (string.IsNullOrWhiteSpace(token))
	{
		token = context.Request.Cookies[jwtOptions.Value.CookieName];
	}

	if (string.IsNullOrWhiteSpace(token))
	{
		return Results.Ok(new ValidateSessionResponse { IsValid = false });
	}

	var response = await accountManager.ValidateSessionAsync(token, cancellationToken);
	return Results.Ok(response);
}

static async Task<IResult> GetMeAsync(HttpContext context, IAccountManager accountManager, CancellationToken cancellationToken)
{
	var userId = GetUserId(context.User);

	if (userId == null)
	{
		return Results.Unauthorized();
	}

	var profile = await accountManager.GetByUserIdAsync(userId.Value, cancellationToken);
	return profile == null ? Results.NotFound() : Results.Ok(profile);
}

static async Task<IResult> UpdateMeAsync(
	UpdateProfileRequest request,
	HttpContext context,
	IAccountManager accountManager,
	CancellationToken cancellationToken)
{
	var userId = GetUserId(context.User);

	if (userId == null)
	{
		return Results.Unauthorized();
	}

	try
	{
		var profile = await accountManager.UpdateAsync(userId.Value, request, cancellationToken);
		return Results.Ok(profile);
	}
	catch (InvalidOperationException ex)
	{
		return Results.NotFound(ex.Message);
	}
}

static async Task<IResult> DeleteMeAsync(HttpContext context, IAccountManager accountManager, CancellationToken cancellationToken)
{
	var userId = GetUserId(context.User);

	if (userId == null)
	{
		return Results.Unauthorized();
	}

	try
	{
		await accountManager.RequestDeleteAsync(userId.Value, cancellationToken);
		return Results.Accepted();
	}
	catch (InvalidOperationException ex)
	{
		return Results.NotFound(ex.Message);
	}
}

static Guid? GetUserId(ClaimsPrincipal principal)
{
	var value = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
	return Guid.TryParse(value, out var userId) ? userId : null;
}
