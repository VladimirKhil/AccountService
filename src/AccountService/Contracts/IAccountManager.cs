using AccountService.Contract.Models;
using AccountService.Contract.Requests;
using AccountService.Contract.Responses;

namespace AccountService.Contracts;

public interface IAccountManager
{
    Task<AuthResponse> LoginBySteamAsync(string authTicket, CancellationToken cancellationToken);

    Task<string> CreateSessionTokenAsync(Guid userId, string username, string displayName, AuthProvider authProvider, CancellationToken cancellationToken);

    Task<UserProfileResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserProfileResponse> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);

    Task RequestDeleteAsync(Guid userId, CancellationToken cancellationToken);

    Task<ValidateSessionResponse> ValidateSessionAsync(string token, CancellationToken cancellationToken);

    Task<int> PurgeDeletedAccountsAsync(CancellationToken cancellationToken);

    Task<int> PurgeExpiredSessionsAsync(CancellationToken cancellationToken);
}
