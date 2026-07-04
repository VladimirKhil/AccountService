using AccountService.Services;

namespace AccountService.Contracts;

public interface ISteamAuthorizationService
{
    Task<SteamAuthorizationResult> AuthorizeAsync(string authTicket, CancellationToken cancellationToken = default);
}
