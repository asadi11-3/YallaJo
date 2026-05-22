using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentBlogs.Infrastructure.Repositories;

public class CreatorPostRepository(ContentBlogsDbContext context)
    : EfRepository<CreatorPost, Guid>(context), ICreatorPostRepository
{
    public Task<CreatorPost?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return context.CreatorPosts
            .FirstOrDefaultAsync(p => p.Slug == normalizedSlug, cancellationToken);
    }

    public Task<bool> IsSlugTakenAsync(
        string slug,
        Guid? excludePostId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return context.CreatorPosts
            .AnyAsync(
                p => p.Slug == normalizedSlug &&
                     (!excludePostId.HasValue || p.Id != excludePostId.Value),
                cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetAllSlugsAsync(
        CancellationToken cancellationToken = default)
    {
        var slugs = await context.CreatorPosts
            .Select(p => p.Slug)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return slugs.ToHashSet();
    }

    public Task<int> CountPublishedByCreatorAsync(
        Guid creatorProfileId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorPosts
            .CountAsync(
                p => p.CreatorProfileId == creatorProfileId &&
                     p.Status == CreatorPostStatus.Published,
                cancellationToken);
    }

    public Task<int> CountFeaturedAsync(CancellationToken cancellationToken = default)
    {
        return context.CreatorPosts
            .CountAsync(
                p => p.IsFeatured &&
                     (p.FeaturedUntil == null || p.FeaturedUntil > DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<List<CreatorPost>> GetExpiredFeaturedAsync(
        DateTime utcNow,
        int batchSize = 200,
        CancellationToken cancellationToken = default)
    {
        return await context.CreatorPosts
            .Where(p => p.IsFeatured && p.FeaturedUntil != null && p.FeaturedUntil <= utcNow)
            .OrderBy(p => p.FeaturedUntil)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> AtomicIncrementPublishedPostCountAsync(
        Guid creatorProfileId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorProfiles
            .Where(p => p.Id == creatorProfileId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.PublishedPostCount, p => p.PublishedPostCount + 1)
                      .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }

    public Task<int> AtomicDecrementPublishedPostCountAsync(
        Guid creatorProfileId,
        CancellationToken cancellationToken = default)
    {
        return context.CreatorProfiles
            .Where(p => p.Id == creatorProfileId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(
                          p => p.PublishedPostCount,
                          p => p.PublishedPostCount > 0 ? p.PublishedPostCount - 1 : 0)
                      .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }
}
