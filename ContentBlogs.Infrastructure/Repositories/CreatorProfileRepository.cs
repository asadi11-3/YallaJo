using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentBlogs.Infrastructure.Repositories;

public class CreatorProfileRepository(ContentBlogsDbContext context)
    : EfRepository<CreatorProfile, Guid>(context), ICreatorProfileRepository
{
    public Task<CreatorProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorProfiles
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.Status == CreatorProfileStatus.Active,
                cancellationToken);
    }

    public Task<CreatorProfile?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return context.CreatorProfiles
            .FirstOrDefaultAsync(p => p.Slug == normalizedSlug, cancellationToken);
    }

    public Task<bool> IsSlugTakenAsync(
        string slug,
        Guid? excludeProfileId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return context.CreatorProfiles
            .AnyAsync(
                p => p.Slug == normalizedSlug &&
                     (!excludeProfileId.HasValue || p.Id != excludeProfileId.Value),
                cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetAllSlugsAsync(
        CancellationToken cancellationToken = default)
    {
        var slugs = await context.CreatorProfiles
            .Select(p => p.Slug)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return slugs.ToHashSet();
    }

    /// <inheritdoc />
    public Task<int> AtomicIncrementFollowerCountAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorProfiles
            .Where(p => p.Id == profileId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.FollowerCount, p => p.FollowerCount + 1)
                      .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AtomicDecrementFollowerCountAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        // CASE WHEN FollowerCount > 0 THEN FollowerCount - 1 ELSE 0 END
        return context.CreatorProfiles
            .Where(p => p.Id == profileId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(
                          p => p.FollowerCount,
                          p => p.FollowerCount > 0 ? p.FollowerCount - 1 : 0)
                      .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AtomicIncrementArticleCountAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorProfiles
            .Where(p => p.Id == profileId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.ArticleCount, p => p.ArticleCount + 1)
                      .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AtomicIncrementReportCountAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorProfiles
            .Where(p => p.Id == profileId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.ReportCount, p => p.ReportCount + 1)
                      .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AtomicDecrementReportCountAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorProfiles
            .Where(p => p.Id == profileId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(
                          p => p.ReportCount,
                          p => p.ReportCount > 0 ? p.ReportCount - 1 : 0)
                      .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<CreatorProfile>> GetTier0PromotionCandidatesAsync(
        int batchSize = 200,
        CancellationToken cancellationToken = default)
    {
        return await context.CreatorProfiles
            .Where(p => p.TrustTier == CreatorTrustTier.New
                     && p.Status == CreatorProfileStatus.Active
                     && !p.EligibleForTier1
                      && p.ArticleCount >= 5
                     && p.ReportRate < 0.05)
            .OrderBy(p => p.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<CreatorProfile>> GetTier1PromotionCandidatesAsync(
        int batchSize = 200,
        CancellationToken cancellationToken = default)
    {
        return await context.CreatorProfiles
            .Where(p => p.TrustTier == CreatorTrustTier.Trusted
                     && p.Status == CreatorProfileStatus.Active
                     && !p.EligibleForTier2
                      && p.ArticleCount >= 25
                     && p.ReportRate < 0.02
                     && p.TotalReactionCount >= 500)
            .OrderBy(p => p.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<CreatorProfile>> GetAutoDemotionCandidatesAsync(
        double reportRateThreshold = 0.15,
        int batchSize = 200,
        CancellationToken cancellationToken = default)
    {
        return await context.CreatorProfiles
            .Where(p => p.TrustTier > CreatorTrustTier.New
                     && p.Status == CreatorProfileStatus.Active
                     && p.ReportRate > reportRateThreshold)
            .OrderByDescending(p => p.ReportRate)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
