using AccountService.Contract;
using AccountService.Contract.Requests;
using AccountService.Contract.Responses;
using System.Net.Http.Json;

namespace AccountService.Client;

public sealed class AccountServiceClient(HttpClient client) : IAccountServiceClient
{
    public async Task<AuthResponse> LoginBySteamAsync(SteamLoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync("auth/steam", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken))!;
    }

    public async Task<UserProfileResponse?> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync("account/me", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfileResponse>(cancellationToken);
    }

    public async Task<UserProfileResponse> UpdateMeAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var response = await client.PatchAsJsonAsync("account/me", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserProfileResponse>(cancellationToken))!;
    }

    public async Task RequestDeleteMeAsync(CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsync("account/me/delete", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ValidateSessionResponse> ValidateSessionAsync(ValidateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync("admin/validate", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ValidateSessionResponse>(cancellationToken))!;
    }
}
