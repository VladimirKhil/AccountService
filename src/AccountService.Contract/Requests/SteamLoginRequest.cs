namespace AccountService.Contract.Requests;

public sealed class SteamLoginRequest
{
    public string AuthTicket { get; set; } = string.Empty;
}
