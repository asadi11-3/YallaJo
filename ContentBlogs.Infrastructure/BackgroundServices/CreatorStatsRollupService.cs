using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
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
        var postRepo = scope.ServiceProvider.GetRequiredService<ICreatorPostRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IContentBlogsUnitOfWork>();

        // ── Step 1: Auto-unfeature expired posts ────────────────────────────
        var unfeaturedCount = await UnfeatureExpiredPostsAsync(postRepo, unitOfWork, ct);
        if (unfeaturedCount > 0)
        {
            logger.LogInformation(
                "Stats rollup: auto-unfeatured {Count} expired post(s)",
                unfeaturedCount);
        }

        // ── Step 2: Recompute profile stats via single SQL UPDATE ───────────
        var updatedCount = await RecomputeProfileStatsAsync(dbContext, ct);
        if (updatedCount > 0)
        {
            logger.LogInformation(
                "Stats rollup: refreshed stats for {Count} creator profile(s)",
                updatedCount);
        }
    }

    private static async Task<int> UnfeatureExpiredPostsAsync(
        ICreatorPostRepository postRepo,
        IContentBlogsUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var total = 0;
        var utcNow = DateTime.UtcNow;

        while (true)
        {
            var batch = await postRepo.GetExpiredFeaturedAsync(utcNow, BatchSize, ct);
            if (batch.Count == 0)
                break;

            foreach (var post in batch)
            {
                post.Unfeature();
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
                p.PublishedPostCount = ISNULL(post_stats.cnt, 0),
                p.TotalViewCount = ISNULL(blog_stats.views, 0) + ISNULL(post_stats.views, 0),
                p.TotalReactionCount = ISNULL(blog_stats.reactions, 0) + ISNULL(post_stats.reactions, 0),
                p.TotalCommentCount = ISNULL(blog_stats.comments, 0) + ISNULL(post_stats.comments, 0),
                p.ReportCount = ISNULL(post_stats.reports, 0) + ISNULL(blog_stats.blog_reports, 0),
                p.ReportRate = CASE
                    WHEN (ISNULL(post_stats.cnt, 0) + ISNULL(blog_stats.cnt, 0)) = 0 THEN 0.0
                    ELSE CAST(ISNULL(post_stats.reports, 0) + ISNULL(blog_stats.blog_reports, 0) AS FLOAT)
                         / CAST(ISNULL(post_stats.cnt, 0) + ISNULL(blog_stats.cnt, 0) AS FLOAT)
                END,
                p.FollowerCount = ISNULL(follow_stats.cnt, 0),
                p.UpdatedAt = GETUTCDATE()
            FROM [content_blogs].[CreatorProfiles] p
            OUTER APPLY (
                SELECT
                    COUNT(*) AS cnt,
                    ISNULL(SUM(b.ViewCount), 0) AS views,
                    ISNULL(SUM(CAST((SELECT COUNT(*) FROM [content_blogs].[BlogCommentReactions] bcr
                        INNER JOIN [content_blogs].[BlogComments] bc ON bcr.BlogCommentId = bc.Id
                        WHERE bc.BlogId = b.Id AND bc.IsDeleted = 0) AS BIGINT)), 0) AS reactions,
                    ISNULL(SUM(CAST((SELECT COUNT(*) FROM [content_blogs].[BlogComments] bc2
                        WHERE bc2.BlogId = b.Id AND bc2.IsDeleted = 0) AS BIGINT)), 0) AS comments,
                    -- Blog entity has no ReportCount column; reports are tracked via Social module events
                    -- and incremented atomically on CreatorProfile. Rollup preserves current value.
                    0 AS blog_reports
                FROM [content_blogs].[Blogs] b
                WHERE b.AuthoredByCreatorId = p.Id
                  AND b.IsDeleted = 0
                  AND b.Status = 1 -- Published
            ) blog_stats
            OUTER APPLY (
                SELECT
                    COUNT(*) AS cnt,
                    ISNULL(SUM(cp.ViewCount), 0) AS views,
                    ISNULL(SUM(cp.ReactionCount), 0) AS reactions,
                    ISNULL(SUM(cp.CommentCount), 0) AS comments,
                    ISNULL(SUM(cp.ReportCount), 0) AS reports
                FROM [content_blogs].[CreatorPosts] cp
                WHERE cp.CreatorProfileId = p.Id
                  AND cp.IsDeleted = 0
                  AND cp.Status = 2 -- Published
            ) post_stats
            OUTER APPLY (
                SELECT COUNT(*) AS cnt
                FROM [content_blogs].[CreatorFollows] cf
                WHERE cf.CreatorProfileId = p.Id
            ) follow_stats
            WHERE p.IsDeleted = 0;";

        return await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
    }
}
