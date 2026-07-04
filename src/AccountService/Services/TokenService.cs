using AccountService.Configuration;
using AccountService.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AccountService.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;
    private readonly JwtSecurityTokenHandler _handler = new();

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        var (privatePem, publicPem) = ResolveKeyPair(_options);

        var privateRsa = RSA.Create();
        privateRsa.ImportFromPem(privatePem);

        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(publicPem);

        _privateKey = new RsaSecurityKey(privateRsa);
        _publicKey = new RsaSecurityKey(publicRsa);
    }

    public TokenIssueResult Issue(Guid userId, string username, string jwtId)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_options.ExpiresMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
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

    private static (string privatePem, string publicPem) ResolveKeyPair(JwtOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PrivateKeyPem) && !string.IsNullOrWhiteSpace(options.PublicKeyPem))
        {
            return (options.PrivateKeyPem, options.PublicKeyPem);
        }

        throw new InvalidOperationException(
            "JWT key pair is not configured. Set Jwt:PrivateKeyPem and Jwt:PublicKeyPem.");
    }
}
