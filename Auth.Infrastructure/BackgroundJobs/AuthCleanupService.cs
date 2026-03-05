using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.BackgroundJobs;


internal sealed class AuthCleanupService(
    IServiceProvider serviceProvider,
    ILogger<AuthCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("AuthCleanupService started");

        try { await Task.Delay(InitialDelay, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "AuthCleanupService: Unexpected error during cleanup cycle");
            }

            try { await Task.Delay(Interval, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }

        logger.LogInformation("AuthCleanupService stopped");
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var now = DateTime.UtcNow;

        var refreshTokensDeleted = await dbContext.RefreshTokens
            .Where(rt => !rt.IsDeleted && (rt.IsRevoked || rt.ExpiresAt < now))
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("AuthCleanup: Deleted {Count} expired/revoked RefreshTokens", refreshTokensDeleted);

        var sessionsDeleted = await dbContext.Sessions
            .Where(s => !s.IsDeleted && (s.IsRevoked || s.ExpiresAt < now))
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("AuthCleanup: Deleted {Count} expired/revoked Sessions", sessionsDeleted);

        var otpsDeleted = await dbContext.Otps
            .Where(o => !o.IsDeleted && (o.IsUsed || o.ExpiresAt < now))
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("AuthCleanup: Deleted {Count} used/expired OTPs", otpsDeleted);
    }
}
