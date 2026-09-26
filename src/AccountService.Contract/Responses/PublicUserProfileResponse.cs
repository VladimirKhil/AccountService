using AccountService.Contract.Models;

namespace AccountService.Contract.Responses;

public sealed class UserProfileResponse
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public byte[]? Avatar { get; set; }

    public Gender Gender { get; set; }
}
