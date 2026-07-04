using AccountService.Contracts;
using AccountService.Database;
using LinqToDB;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace AccountService.Configuration;

public sealed class ConfigureJwtBearerOptions(
    IOptions<JwtOptions> jwtOptions,
    ITokenService tokenService) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        var jwt = jwtOptions.Value;

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var cookieToken = context.Request.Cookies[jwt.CookieName];

                if (!string.IsNullOrWhiteSpace(cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;

                var userIdRaw = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var jwtId = principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (!Guid.TryParse(userIdRaw, out var userId) || string.IsNullOrWhiteSpace(jwtId))
                {
                    context.Fail("Token claims are invalid");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AccountDbConnection>();
                var now = DateTimeOffset.UtcNow;
                var cancellationToken = context.HttpContext.RequestAborted;

                var sessionExists = await db.AccountSessions.AnyAsync(
                    s => s.JwtId == jwtId && s.AccountId == userId && s.RevokedAt == null && s.ExpiresAt > now,
                    token: cancellationToken);

                if (!sessionExists)
                {
                    context.Fail("Session is revoked or expired");
                    return;
                }

                var accountExists = await db.Accounts.AnyAsync(
                    a => a.Id == userId && a.DeletedAt == null,
                    token: cancellationToken);

                if (!accountExists)
                {
                    context.Fail("Account is not active");
                }
            },
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = tokenService.GetPublicSigningKey(),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    }
}
