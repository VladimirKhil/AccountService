using AccountService.Contract.Requests;
using AccountService.Contract.Responses;

namespace AccountService.Contract;

public interface IAccountServiceClient
{
    Task<AuthResponse> LoginBySteamAsync(SteamLoginRequest request, CancellationToken cancellationToken = default);

    Task<UserProfileResponse?> GetMeAsync(CancellationToken cancellationToken = default);

    Task<UserProfileResponse> UpdateMeAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task RequestDeleteMeAsync(CancellationToken cancellationToken = default);

    Task<ValidateSessionResponse> ValidateSessionAsync(ValidateSessionRequest request, CancellationToken cancellationToken = default);
}
