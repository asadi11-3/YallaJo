using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContentBlogs.Infrastructure.BackgroundServices;

/// <summary>
/// Daily background service (03:00 UTC) that expires stale creator invitations
/// whose ExpiresAt has passed while still in Pending status.
/// Processes in batches of 200 to avoid long-running transactions.
/// </summary>
public sealed class CreatorInvitationCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<CreatorInvitationCleanupService> logger) : BackgroundService
{
    private const int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait until the next 03:00 UTC before first run
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireStaleInvitationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Creator invitation cleanup job failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ExpireStaleInvitationsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICreatorInvitationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Application.Interfaces.IContentBlogsUnitOfWork>();

        var utcNow = DateTime.UtcNow;
        var totalExpired = 0;

        while (true)
        {
            var batch = await repo.GetExpiredPendingAsync(utcNow, BatchSize, ct);
            if (batch.Count == 0)
                break;

            foreach (var invitation in batch)
            {
                invitation.Expire();
            }

            await unitOfWork.SaveChangesAsync(ct);
            totalExpired += batch.Count;

            if (batch.Count < BatchSize)
                break;
        }

        if (totalExpired > 0)
        {
            logger.LogInformation(
                "Creator invitation cleanup: expired {Count} stale invitation(s)",
                totalExpired);
        }
    }

    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(3);
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        var delay = nextRun - now;
        await Task.Delay(delay, ct);
    }
}
