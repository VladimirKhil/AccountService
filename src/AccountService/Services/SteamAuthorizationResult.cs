namespace AccountService.Services;

public sealed record SteamAuthorizationResult(
    bool IsSuccess,
    string? SteamId = null,
    string? SteamName = null,
    byte[]? Avatar = null,
    string? Error = null,
    bool IsServiceError = false);
