# Validate AccountService Cookies In Another C# Service

This guide explains the recommended way to validate cookies issued by AccountService from another ASP.NET Core service.

## Why call AccountService instead of validating JWT locally

AccountService validation checks more than JWT signature/lifetime:

- token is signed and not expired
- session is present and not revoked
- user account still exists and is not deleted

For this reason, use AccountService `/api/v1/admin/validate` as the source of truth.

## How validation works

1. Your service receives an incoming request with the auth cookie (default name: `account_auth`).
2. Your service extracts the cookie token.
3. Your service calls AccountService `POST /api/v1/admin/validate` with:
   - Basic auth header: `admin:{ClientSecret}`
   - body: `{ "token": "..." }`
4. AccountService returns `ValidateSessionResponse`:
   - `IsValid = true` and user info when session is active
   - `IsValid = false` when token/session/user is invalid

## Required configuration

In the consuming service (`appsettings.json`):

```json
{
  "AccountServiceClient": {
    "ServiceUri": "https://account-service-host/",
    "ClientSecret": "your-shared-admin-secret",
    "Timeout": "00:00:15"
  },
  "AccountAuth": {
    "CookieName": "account_auth"
  }
}
```

Notes:

- `ClientSecret` must match `Admin:ClientSecret` configured in AccountService.
- `CookieName` should match `Jwt:CookieName` configured in AccountService.

## Option A (recommended): use AccountService.Client

### 1) Add references

Reference these projects or packages in your service:

- `AccountService.Client`
- `AccountService.Contract`

### 2) Register the client

In `Program.cs`:

```csharp
using AccountService.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAccountServiceClient(builder.Configuration);
```

### 3) Validate cookie per request

Example helper service:

```csharp
using AccountService.Contract;
using AccountService.Contract.Requests;

public sealed class AccountCookieValidator(
    IAccountServiceClient accountClient,
    IConfiguration configuration)
{
    public async Task<(bool IsValid, Guid? UserId, string? Username)> ValidateAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var cookieName = configuration["AccountAuth:CookieName"] ?? "account_auth";

        if (!httpContext.Request.Cookies.TryGetValue(cookieName, out var token) || string.IsNullOrWhiteSpace(token))
        {
            return (false, null, null);
        }

        var result = await accountClient.ValidateSessionAsync(
            new ValidateSessionRequest { Token = token },
            cancellationToken);

        return (result.IsValid, result.UserId, result.Username);
    }
}
```

Usage in endpoint:

```csharp
app.MapGet("/internal/me", async (
    HttpContext context,
    AccountCookieValidator validator,
    CancellationToken cancellationToken) =>
{
    var validation = await validator.ValidateAsync(context, cancellationToken);

    if (!validation.IsValid || validation.UserId == null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        validation.UserId,
        validation.Username,
    });
});
```

## Option B: call endpoint with plain HttpClient

If you do not want to use `AccountService.Client`, call the endpoint directly:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

public sealed class RawAccountValidationClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<bool> IsValidAsync(string token, CancellationToken cancellationToken = default)
    {
        var secret = configuration["AccountServiceClient:ClientSecret"]
            ?? throw new InvalidOperationException("Missing AccountServiceClient:ClientSecret");

        var raw = Encoding.ASCII.GetBytes($"admin:{secret}");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(raw));

        var response = await httpClient.PostAsJsonAsync(
            "api/v1/admin/validate",
            new { token },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var model = await response.Content.ReadFromJsonAsync<ValidateSessionResponse>(cancellationToken);
        return model?.IsValid == true;
    }
}

public sealed class ValidateSessionResponse
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
}
```

## Option C: validate locally (no HTTP call)

You can validate the cookie locally in your service, but there is a tradeoff.

What local validation can do:

- verify JWT signature
- verify issuer and audience
- verify expiration

What local validation cannot do by itself:

- confirm session was not revoked
- confirm user account still exists and is not deleted

To reduce this gap, check token `jti` during auth against one of these revocation stores:

- distributed cache (Redis) for multi-node or restart-safe behavior
- in-memory cache for single-node simplicity

### Prerequisites for local validation

1. Configure stable keys in AccountService (`Jwt:PrivateKeyPem` and `Jwt:PublicKeyPem`).
2. Share only the public key with consumer services.
3. Keep `Jwt:Issuer`, `Jwt:Audience`, and cookie name aligned across services.

Important:

- If AccountService runs without configured keys, it generates keys in memory.
- In-memory keys change on restart and break local validation in other services.

### Example configuration in consumer service

```json
{
  "Jwt": {
    "Issuer": "AccountService",
    "Audience": "AccountConsumers",
    "PublicKeyPem": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"
  },
  "AccountAuth": {
    "CookieName": "account_auth"
  }
}
```

### Program.cs with local JWT validation and distributed jti revocation check (Redis)

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

var publicKeyPem = builder.Configuration["Jwt:PublicKeyPem"]
    ?? throw new InvalidOperationException("Missing Jwt:PublicKeyPem");

var rsa = RSA.Create();
rsa.ImportFromPem(publicKeyPem);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),

            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var cookieName = context.HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()["AccountAuth:CookieName"] ?? "account_auth";

                if (context.Request.Cookies.TryGetValue(cookieName, out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Missing token jti");
                    return;
                }

                var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
                var revoked = await cache.GetStringAsync($"account:revoked-jti:{jti}");

                if (!string.IsNullOrEmpty(revoked))
                {
                    context.Fail("Token session is revoked");
                }
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

### Writing revocations into shared cache

When a user logs out, session is revoked, or account is deleted, publish revoked `jti` into shared cache with TTL at least until token expiration.

Example key format:

- `account:revoked-jti:{jti}`

If AccountService already persists sessions in database, a background sync job can mirror revoked sessions into Redis.

### Program.cs with local JWT validation and single-node jti revocation check (IMemoryCache)

Use this variant when your consumer service is single-node and you do not want Redis.

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

var publicKeyPem = builder.Configuration["Jwt:PublicKeyPem"]
    ?? throw new InvalidOperationException("Missing Jwt:PublicKeyPem");

var rsa = RSA.Create();
rsa.ImportFromPem(publicKeyPem);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),

            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var cookieName = context.HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()["AccountAuth:CookieName"] ?? "account_auth";

                if (context.Request.Cookies.TryGetValue(cookieName, out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Missing token jti");
                    return Task.CompletedTask;
                }

                var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

                if (cache.TryGetValue($"account:revoked-jti:{jti}", out _))
                {
                    context.Fail("Token session is revoked");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

Single-node caveat:

- in-memory revocations are lost on service restart
- if that is not acceptable, use Redis or database-backed revocation checks

### When to choose local validation

- choose local validation with Redis when you need restart-safe revocation and may scale to multiple nodes
- choose local validation with IMemoryCache for single-node deployments with minimal infrastructure
- choose admin HTTP validation when you need strict source-of-truth checks with minimal integration complexity

## Security recommendations

- Keep `Admin:ClientSecret` and consuming-service `ClientSecret` in secret storage, not in source control.
- Use HTTPS between services.
- Keep auth cookie `HttpOnly` and `Secure` (already set by AccountService).
- Prefer forwarding only the cookie token value, not full incoming Cookie headers.
- Treat `IsValid = false` as unauthenticated, not as an internal error.

## Troubleshooting

- `401 Unauthorized` from `/api/v1/admin/validate`:
  - check Basic auth header
  - verify shared secret matches AccountService `Admin:ClientSecret`
- `503 ServiceUnavailable` from admin endpoint:
  - AccountService has empty `Admin:ClientSecret`
- `IsValid = false` with apparently correct token:
  - token expired
  - session revoked
  - account soft-deleted
  - cookie name mismatch between services
