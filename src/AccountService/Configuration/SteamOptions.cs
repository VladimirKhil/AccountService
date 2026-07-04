namespace AccountService.Configuration;

public sealed class SteamOptions
{
    public const string ConfigurationSectionName = "Steam";

    public string ApiKey { get; set; } = string.Empty;

    public uint AppId { get; set; }

    public string Identity { get; set; } = "account-service";

    public string BaseAddress { get; set; } = "https://api.steampowered.com/";
}
