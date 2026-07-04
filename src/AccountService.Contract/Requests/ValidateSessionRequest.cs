namespace AccountService.Contract.Requests;

public sealed class ValidateSessionRequest
{
    public string? Token { get; set; }
}
