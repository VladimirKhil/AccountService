namespace AccountService.Configuration;

public sealed class AdminOptions
{
    public const string ConfigurationSectionName = "Admin";

    public string ClientSecret { get; set; } = string.Empty;
}
