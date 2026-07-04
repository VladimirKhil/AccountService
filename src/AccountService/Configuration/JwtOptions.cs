namespace AccountService.Configuration;

public sealed class JwtOptions
{
    public const string ConfigurationSectionName = "Jwt";

    public string Issuer { get; set; } = "AccountService";

    public string Audience { get; set; } = "AccountConsumers";

    public string? PrivateKeyPem { get; set; }

    public string? PublicKeyPem { get; set; }

    public int ExpiresMinutes { get; set; } = 120;

    public string CookieName { get; set; } = "account_auth";
}
