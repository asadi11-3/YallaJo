using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentBlogs.Domain.Repositories;

public interface ICreatorPostRepository : IRepository<CreatorPost, Guid>
{
    /// <summary>
    /// Gets a post by its URL-friendly slug.
    /// </summary>
    Task<CreatorPost?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the slug is already taken by another post.
    /// </summary>
    Task<bool> IsSlugTakenAsync(
        string slug,
        Guid? excludePostId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all slugs currently in use for creator posts.
    /// Used by the slug generator to guarantee uniqueness.
    /// </summary>
    Task<IReadOnlySet<string>> GetAllSlugsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all published posts for a given creator profile.
    /// Used by tier-promotion eligibility checks.
    /// </summary>
    Task<int> CountPublishedByCreatorAsync(
        Guid creatorProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of currently featured posts (globally).
    /// Used to enforce the soft cap of <see cref="CreatorPost.MaxFeaturedGlobal"/>.
    /// </summary>
    Task<int> CountFeaturedAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns posts whose <see cref="CreatorPost.FeaturedUntil"/> has passed.
    /// Used by the stats-rollup background service to auto-unfeature expired posts.
    /// </summary>
    Task<List<CreatorPost>> GetExpiredFeaturedAsync(
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments the published-post count for a creator profile.
    /// Returns the number of rows affected (0 if profile not found).
    /// </summary>
    Task<int> AtomicIncrementPublishedPostCountAsync(
        Guid creatorProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically decrements the published-post count for a creator profile.
    /// Prevents negative values. Returns the number of rows affected.
    /// </summary>
    Task<int> AtomicDecrementPublishedPostCountAsync(
        Guid creatorProfileId,
        CancellationToken cancellationToken = default);
}
