using ContentBlogs.Domain.Entities.Creators;

namespace ContentBlogs.Domain.Repositories;

/// <summary>
/// Repository for <see cref="CreatorFollow"/> junction entities.
/// CreatorFollow is a BaseEntity (hard-delete, no soft-delete), so it does not
/// implement IAggregateRoot and cannot use <see cref="YallaJo.SharedKernel.Domain.Abstractions.Data.IRepository{TEntity,TKey}"/>.
/// </summary>
public interface ICreatorFollowRepository
{
    /// <summary>
    /// Gets an existing follow relationship, or null if the user is not following the creator.
    /// </summary>
    Task<CreatorFollow?> GetAsync(
        Guid followerUserId,
        Guid creatorProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the user is already following the creator.
    /// </summary>
    Task<bool> ExistsAsync(
        Guid followerUserId,
        Guid creatorProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new follow relationship.
    /// </summary>
    Task AddAsync(
        CreatorFollow follow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes the follow relationship.
    /// </summary>
    void Remove(CreatorFollow follow);

    /// <summary>
    /// Returns the total number of followers for a given creator profile.
    /// </summary>
    Task<int> CountByCreatorProfileIdAsync(
        Guid creatorProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the follower user IDs for a given creator profile (paginated).
    /// </summary>
    Task<List<Guid>> GetFollowerUserIdsAsync(
        Guid creatorProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the followed-at timestamps for a creator's followers (paginated,
    /// newest first). Used by the public-safe followers endpoint (Gap 3 Phase A)
    /// which must never expose follower user IDs.
    /// </summary>
    Task<List<DateTime>> GetFollowerTimestampsAsync(
        Guid creatorProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
