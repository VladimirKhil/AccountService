namespace AccountService.Contract.Responses;

public sealed class AuthResponse
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Populated only when the login request includes <c>includeToken=true</c>.
    /// Intended for non-browser clients (e.g. Tauri) that cannot use HttpOnly cookies
    /// and need to send the token via the <c>Authorization: Bearer</c> header instead.
    /// </summary>
    public string? Token { get; set; }
}
