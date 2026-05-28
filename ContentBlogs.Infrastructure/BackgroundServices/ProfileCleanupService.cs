using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContentBlogs.Infrastructure.BackgroundServices;

/// <summary>
/// Daily background service (02:30 UTC) that hard-deletes soft-deleted CreatorProfiles
/// whose DeletedAt + 60 days &lt; now. Cascades: anonymizes author on blogs, removes follows.
/// </summary>
public sealed class ProfileCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ProfileCleanupService> logger) : BackgroundService
{
    private const int RetentionDays = 60;
    private const int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await HardDeleteProfilesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Creator profile hard-delete cleanup job failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task HardDeleteProfilesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContentBlogsDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
        var totalDeleted = 0;

        while (true)
        {
            var profiles = await dbContext.CreatorProfiles
                .Where(p => p.IsDeleted && p.DeletedAt != null && p.DeletedAt < cutoff)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (profiles.Count == 0)
                break;

            // Anonymize authored blogs: clear AuthoredByCreatorId for all blogs by this creator
            foreach (var profile in profiles)
            {
                var authoredBlogs = await dbContext.Blogs
                    .Where(b => b.AuthoredByCreatorId == profile.Id)
                    .ToListAsync(ct);

                // Note: AuthoredByCreatorId is a private setter — use EF direct update
                await dbContext.Blogs
                    .Where(b => b.AuthoredByCreatorId == profile.Id)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(b => b.AuthoredByCreatorId, (Guid?)null),
                        ct);
            }

            dbContext.CreatorProfiles.RemoveRange(profiles);
            await dbContext.SaveChangesAsync(ct);
            totalDeleted += profiles.Count;

            if (profiles.Count < BatchSize)
                break;
        }

        if (totalDeleted > 0)
        {
            logger.LogInformation(
                "Creator profile cleanup: hard-deleted {Count} profile(s) older than {Days} days",
                totalDeleted, RetentionDays);
        }
    }

    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(2).AddMinutes(30);
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        await Task.Delay(nextRun - now, ct);
    }
}
