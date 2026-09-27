using AccountService.Contracts;

namespace AccountService.Services;

public sealed class PurgeDeletedAccountsHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<PurgeDeletedAccountsHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var accountManager = scope.ServiceProvider.GetRequiredService<IAccountManager>();

                var expiredSessions = await accountManager.PurgeExpiredSessionsAsync(stoppingToken);
                if (expiredSessions > 0)
                {
                    logger.LogInformation("Purged {Count} expired sessions", expiredSessions);
                }

                var purged = await accountManager.PurgeDeletedAccountsAsync(stoppingToken);
                if (purged > 0)
                {
                    logger.LogInformation("Purged {Count} accounts", purged);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to purge deleted accounts or expired sessions");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
