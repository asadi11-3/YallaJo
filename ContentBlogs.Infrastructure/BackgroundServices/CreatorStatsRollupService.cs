using ContentBlogs.Application.Interfaces;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContentBlogs.Infrastructure.BackgroundServices;

/// <summary>
/// Hourly background service that recomputes denormalized stats on each
/// <see cref="Domain.Entities.Creators.CreatorProfile"/> and auto-unfeatures
/// creator posts whose <c>FeaturedUntil</c> has passed.
/// <para>
/// Recomputed stats: ArticleCount, PublishedPostCount, TotalViewCount,
/// TotalReactionCount, TotalCommentCount, ReportCount, ReportRate, FollowerCount.
/// </para>
/// </summary>
/// <summary>
/// Hourly background service that recomputes denormalized stats on each
/// <see cref="Domain.Entities.Creators.CreatorProfile"/> and auto-unfeatures
/// blogs whose <c>FeaturedUntil</c> has passed.
/// <para>
/// Recomputed stats: ArticleCount, TotalViewCount, TotalReactionCount, TotalCommentCount,
/// ReportCount, ReportRate, FollowerCount.
/// </para>
/// </summary>
public sealed class CreatorStatsRollupService(
    IServiceScopeFactory scopeFactory,
    ILogger<CreatorStatsRollupService> logger) : BackgroundService
{
    private const int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small initial delay to let the app fully start
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRollupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Creator stats rollup job failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task RunRollupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContentBlogsDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IContentBlogsUnitOfWork>();

        // ── Step 1: Auto-unfeature expired blogs ─────────────────────────────
        var unfeaturedCount = await UnfeatureExpiredBlogsAsync(dbContext, unitOfWork, ct);
        if (unfeaturedCount > 0)
        {
            logger.LogInformation(
                "Stats rollup: auto-unfeatured {Count} expired blog(s)",
                unfeaturedCount);
        }

        // ── Step 2: Recompute profile stats via single SQL UPDATE ──────────
        var updatedCount = await RecomputeProfileStatsAsync(dbContext, ct);
        if (updatedCount > 0)
        {
            logger.LogInformation(
                "Stats rollup: refreshed stats for {Count} creator profile(s)",
                updatedCount);
        }
    }

    private static async Task<int> UnfeatureExpiredBlogsAsync(
        ContentBlogsDbContext dbContext,
        IContentBlogsUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var total = 0;
        var utcNow = DateTime.UtcNow;

        while (true)
        {
            var batch = await dbContext.Blogs
                .Where(b => !b.IsDeleted && b.FeaturedAt != null && b.FeaturedUntil != null && b.FeaturedUntil < utcNow)
                .OrderBy(b => b.FeaturedUntil)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                break;

            foreach (var blog in batch)
            {
                blog.Unfeature(utcNow);
            }

            await unitOfWork.SaveChangesAsync(ct);
            total += batch.Count;

            if (batch.Count < BatchSize)
                break;
        }

        return total;
    }

    /// <summary>
    /// Recomputes denormalized stats on CreatorProfiles using a single bulk SQL UPDATE
    /// with correlated subqueries. This avoids loading every profile into memory.
    /// All content stats now come from the Blogs table (CreatorPost was merged into Blog).
    /// </summary>
    private static async Task<int> RecomputeProfileStatsAsync(
        ContentBlogsDbContext dbContext,
        CancellationToken ct)
    {
        // Using raw SQL for an efficient correlated UPDATE across multiple tables.
        // This updates all active creator profiles in a single round-trip.
        var sql = @"
            UPDATE p
            SET
                p.ArticleCount = ISNULL(blog_stats.cnt, 0),
                p.TotalViewCount = ISNULL(blog_stats.views, 0),
                p.TotalReactionCount = ISNULL(blog_stats.reactions, 0),
                p.TotalCommentCount = ISNULL(blog_stats.comments, 0),
                p.ReportCount = ISNULL(blog_stats.reports, 0),
                p.ReportRate = CASE
                    WHEN ISNULL(blog_stats.cnt, 0) = 0 THEN 0.0
                    ELSE CAST(ISNULL(blog_stats.reports, 0) AS FLOAT)
                         / CAST(blog_stats.cnt AS FLOAT)
                END,
                p.FollowerCount = ISNULL(follow_stats.cnt, 0),
                p.UpdatedAt = GETUTCDATE()
            FROM [content_blogs].[CreatorProfiles] p
            OUTER APPLY (
                SELECT
                    COUNT(*) AS cnt,
                    ISNULL(SUM(b.ViewCount), 0) AS views,
                    ISNULL(SUM(b.ReactionCount), 0) AS reactions,
                    ISNULL(SUM(b.CommentCount), 0) AS comments,
                    ISNULL(SUM(b.ReportCount), 0) AS reports
                FROM [content_blogs].[Blogs] b
                WHERE b.AuthoredByCreatorId = p.Id
                  AND b.IsDeleted = 0
                  AND b.Status = 2 -- Published
            ) blog_stats
            OUTER APPLY (
                SELECT COUNT(*) AS cnt
                FROM [content_blogs].[CreatorFollows] cf
                WHERE cf.CreatorProfileId = p.Id
            ) follow_stats
            WHERE p.IsDeleted = 0;";

        return await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
    }
}
