using AccountService.Configuration;
using AccountService.Contract.Models;
using AccountService.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AccountService.Services;

public sealed class TokenService : ITokenService
{
    public const string AuthProviderClaimType = "auth_provider";

    private readonly JwtOptions _options;
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;
    private readonly JwtSecurityTokenHandler _handler = new();

    public TokenService(IOptions<JwtOptions> options, IHostEnvironment hostEnvironment)
    {
        _options = options.Value;
        var (privatePem, publicPem) = ResolveKeyPair(_options, hostEnvironment.IsDevelopment());

        var privateRsa = RSA.Create();
        privateRsa.ImportFromPem(privatePem);

        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(publicPem);

        _privateKey = new RsaSecurityKey(privateRsa);
        _publicKey = new RsaSecurityKey(publicRsa);
    }

    public TokenIssueResult Issue(Guid userId, string externalId, string username, AuthProvider authProvider, string jwtId)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_options.ExpiresMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.NameId, externalId),
                new Claim(JwtRegisteredClaimNames.UniqueName, UsernameFormatter.FormatPublic(username)),
                new Claim(AuthProviderClaimType, authProvider.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, jwtId),
            ]),
            Expires = expires.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256),
        };

        var token = _handler.CreateToken(descriptor);
        return new TokenIssueResult(_handler.WriteToken(token), expires);
    }

    public TokenValidationResult Validate(string token)
    {
        try
        {
            var principal = _handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _publicKey,
                ClockSkew = TimeSpan.FromMinutes(1),
            }, out _);

            var jwtId = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            return new TokenValidationResult(true, principal, jwtId);
        }
        catch
        {
            return new TokenValidationResult(false);
        }
    }

    public SecurityKey GetPublicSigningKey() => _publicKey;

    private static (string privatePem, string publicPem) ResolveKeyPair(JwtOptions options, bool isDevelopment)
    {
        var hasPrivateKey = !string.IsNullOrWhiteSpace(options.PrivateKeyPem);
        var hasPublicKey = !string.IsNullOrWhiteSpace(options.PublicKeyPem);

        if (hasPrivateKey && hasPublicKey)
        {
            return (options.PrivateKeyPem!, options.PublicKeyPem!);
        }

        if (isDevelopment && !hasPrivateKey && !hasPublicKey)
        {
            using var rsa = RSA.Create(2048);
            return (rsa.ExportRSAPrivateKeyPem(), rsa.ExportRSAPublicKeyPem());
        }

        throw new InvalidOperationException(
            "JWT key pair is not configured correctly. Set both Jwt:PrivateKeyPem and Jwt:PublicKeyPem. Temporary development keys are generated only when both values are missing.");
    }
}
