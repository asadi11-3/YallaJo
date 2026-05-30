using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContentBlogs.Infrastructure.BackgroundServices;

/// <summary>
/// Daily background service (02:00 UTC) that hard-deletes soft-deleted Blogs
/// whose DeletedAt + 60 days &lt; now. Only affects blogs where IsDeleted = true.
/// </summary>
public sealed class BlogCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<BlogCleanupService> logger) : BackgroundService
{
    private const int RetentionDays = 60;
    private const int BatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await HardDeleteBlogsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Blog hard-delete cleanup job failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task HardDeleteBlogsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContentBlogsDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
        var totalDeleted = 0;

        while (true)
        {
            var blogs = await dbContext.Blogs
                .Where(b => b.IsDeleted && b.DeletedAt != null && b.DeletedAt < cutoff)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (blogs.Count == 0)
                break;

            dbContext.Blogs.RemoveRange(blogs);
            await dbContext.SaveChangesAsync(ct);
            totalDeleted += blogs.Count;

            if (blogs.Count < BatchSize)
                break;
        }

        if (totalDeleted > 0)
        {
            logger.LogInformation(
                "Blog cleanup: hard-deleted {Count} blog(s) older than {Days} days",
                totalDeleted, RetentionDays);
        }
    }

    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(2);
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        await Task.Delay(nextRun - now, ct);
    }
}
