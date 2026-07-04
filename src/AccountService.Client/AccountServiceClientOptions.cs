namespace AccountService.Client;

public sealed class AccountServiceClientOptions
{
    public const string ConfigurationSectionName = "AccountServiceClient";

    public Uri? ServiceUri { get; set; }

    public string? ClientSecret { get; set; }

    public int RetryCount { get; set; } = 3;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
}
