using AccountService.Contract.Models;

namespace AccountService.Contract.Requests;

public sealed class UpdateProfileRequest
{
    public string? Username { get; set; }

    public byte[]? Avatar { get; set; }

    public Gender? Gender { get; set; }
}
