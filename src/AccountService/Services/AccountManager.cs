using AccountService.Contract.Models;
using AccountService.Contract.Requests;
using AccountService.Contract.Responses;
using AccountService.Contracts;
using AccountService.Database;
using AccountService.Database.Models;
using LinqToDB;
using System.IdentityModel.Tokens.Jwt;

namespace AccountService.Services;

public sealed class AccountManager(
    AccountDbConnection db,
    ISteamAuthorizationService steamAuthorizationService,
    ITokenService tokenService,
    ILogger<AccountManager> logger) : IAccountManager
{
    private static readonly TimeSpan DeleteRetention = TimeSpan.FromDays(7);

    public async Task<AuthResponse> LoginBySteamAsync(string authTicket, CancellationToken cancellationToken)
    {
        var auth = await steamAuthorizationService.AuthorizeAsync(authTicket, cancellationToken);

        if (!auth.IsSuccess || string.IsNullOrWhiteSpace(auth.SteamId) || string.IsNullOrWhiteSpace(auth.SteamName))
        {
            throw new InvalidOperationException(auth.Error ?? "Steam authorization failed");
        }

        var steamId = auth.SteamId;
        var displayName = auth.SteamName.Trim();
        var now = DateTimeOffset.UtcNow;
        var provider = AuthProvider.Steam;

        var link = await db.ExternalAuthLinks
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderUserId == steamId, token: cancellationToken);

        AccountRecord account;

        if (link == null)
        {
            account = new AccountRecord
            {
                Id = Guid.NewGuid(),
                Username = BuildDefaultUsername(steamId),
                DisplayName = displayName,
                Avatar = auth.Avatar,
                Gender = Gender.Unspecified,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await db.InsertAsync(account, token: cancellationToken);

            link = new ExternalAuthLinkRecord
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Provider = provider,
                ProviderUserId = steamId,
                CreatedAt = now,
            };

            await db.InsertAsync(link, token: cancellationToken);
        }
        else
        {
            account = await db.Accounts.FirstAsync(a => a.Id == link.AccountId, token: cancellationToken);

            if (account.DeletedAt != null)
            {
                account.DeletedAt = null;
                account.PurgeAfter = null;
            }

            if (string.IsNullOrWhiteSpace(account.Username))
            {
                account.Username = BuildDefaultUsername(steamId);
            }

            account.DisplayName = displayName;

            if (account.Avatar == null && auth.Avatar?.Length > 0)
            {
                account.Avatar = auth.Avatar;
            }

            account.UpdatedAt = now;
            await db.UpdateAsync(account, token: cancellationToken);
        }

        return new AuthResponse { UserId = account.Id };
    }

    public async Task<UserProfileResponse?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == userId && a.DeletedAt == null, token: cancellationToken);

        return account == null ? null : Map(account);
    }

    public async Task<UserProfileResponse> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == userId && a.DeletedAt == null, token: cancellationToken)
            ?? throw new InvalidOperationException("Account is not found");

        if (request.Username != null)
        {
            account.Username = request.Username.Trim();
        }

        if (request.Avatar != null)
        {
            account.Avatar = request.Avatar;
        }

        if (request.Gender.HasValue)
        {
            account.Gender = request.Gender.Value;
        }

        account.UpdatedAt = DateTimeOffset.UtcNow;

        await db.UpdateAsync(account, token: cancellationToken);

        return Map(account);
    }

    public async Task RequestDeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == userId && a.DeletedAt == null, token: cancellationToken)
            ?? throw new InvalidOperationException("Account is not found");

        var now = DateTimeOffset.UtcNow;
        account.DeletedAt = now;
        account.PurgeAfter = now.Add(DeleteRetention);
        account.UpdatedAt = now;

        await db.UpdateAsync(account, token: cancellationToken);

        await db.AccountSessions
            .Where(x => x.AccountId == userId && x.RevokedAt == null)
            .Set(x => x.RevokedAt, now)
            .UpdateAsync(cancellationToken);
    }

    public async Task<ValidateSessionResponse> ValidateSessionAsync(string token, CancellationToken cancellationToken)
    {
        var tokenValidation = tokenService.Validate(token);

        if (!tokenValidation.IsValid || tokenValidation.Principal == null)
        {
            return new ValidateSessionResponse { IsValid = false };
        }

        var userIdRaw = tokenValidation.Principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(userIdRaw, out var userId) || string.IsNullOrWhiteSpace(tokenValidation.JwtId))
        {
            return new ValidateSessionResponse { IsValid = false };
        }

        var now = DateTimeOffset.UtcNow;
        var session = await db.AccountSessions.FirstOrDefaultAsync(
            s => s.JwtId == tokenValidation.JwtId && s.RevokedAt == null && s.ExpiresAt > now,
            token: cancellationToken);

        if (session == null)
        {
            return new ValidateSessionResponse { IsValid = false };
        }

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == userId && a.DeletedAt == null, token: cancellationToken);

        if (account == null)
        {
            return new ValidateSessionResponse { IsValid = false };
        }

        return new ValidateSessionResponse
        {
            IsValid = true,
            UserId = account.Id,
            Username = account.Username,
        };
    }

    public async Task<int> PurgeDeletedAccountsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var ids = await db.Accounts
            .Where(a => a.DeletedAt != null && a.PurgeAfter != null && a.PurgeAfter <= now)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return 0;
        }

        logger.LogInformation("Purging {Count} accounts", ids.Count);

        await db.AccountSessions.Where(s => ids.Contains(s.AccountId)).DeleteAsync(cancellationToken);
        await db.ExternalAuthLinks.Where(l => ids.Contains(l.AccountId)).DeleteAsync(cancellationToken);
        await db.Accounts.Where(a => ids.Contains(a.Id)).DeleteAsync(cancellationToken);

        return ids.Count;
    }

    public async Task<string> CreateSessionTokenAsync(Guid userId, string username, string displayName, AuthProvider authProvider, CancellationToken cancellationToken)
    {
        var jwtId = Guid.NewGuid().ToString("N");
        var issue = tokenService.Issue(userId, username, displayName, authProvider, jwtId);

        await db.InsertAsync(new AccountSessionRecord
        {
            Id = Guid.NewGuid(),
            AccountId = userId,
            JwtId = jwtId,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = issue.ExpiresAt,
        }, token: cancellationToken);

        return issue.Token;
    }

    private static string BuildDefaultUsername(string steamId) => $"steam_{steamId}";

    private static UserProfileResponse Map(AccountRecord account) => new()
    {
        UserId = account.Id,
        Username = account.Username,
        DisplayName = account.DisplayName,
        Avatar = account.Avatar,
        Gender = account.Gender,
    };
}
