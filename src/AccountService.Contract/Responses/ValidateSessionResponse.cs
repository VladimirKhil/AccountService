namespace AccountService.Contract.Responses;

public sealed class ValidateSessionResponse
{
    public bool IsValid { get; set; }

    public Guid? UserId { get; set; }

    public string? Username { get; set; }
}
