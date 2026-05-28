using ContentBlogs.Domain.Entities.Creators;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentBlogs.Domain.Repositories;

public interface ICreatorProfileRepository : IRepository<CreatorProfile, Guid>
{
    /// <summary>
    /// Gets the active profile for a given user. Returns null if the user is not a creator
    /// or their profile is deactivated.
    /// </summary>
    Task<CreatorProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a profile by its URL-friendly slug.
    /// </summary>
    Task<CreatorProfile?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the slug is already taken by another profile.
    /// </summary>
    Task<bool> IsSlugTakenAsync(
        string slug,
        Guid? excludeProfileId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all slugs currently in use. Used by <see cref="Application.Commands.Creator.Common.CreatorSlugGenerator"/>
    /// to guarantee uniqueness.
    /// </summary>
    Task<IReadOnlySet<string>> GetAllSlugsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments the follower count using a single SQL UPDATE.
    /// Returns the number of rows affected (0 if profile not found).
    /// </summary>
    Task<int> AtomicIncrementFollowerCountAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically decrements the follower count using a single SQL UPDATE.
    /// Prevents negative values by applying a server-side floor of 0.
    /// Returns the number of rows affected (0 if profile not found).
    /// </summary>
    Task<int> AtomicDecrementFollowerCountAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments the article count using a single SQL UPDATE.
    /// Returns the number of rows affected (0 if profile not found).
    /// </summary>
    Task<int> AtomicIncrementArticleCountAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments the report count using a single SQL UPDATE.
    /// Returns the number of rows affected (0 if profile not found).
    /// </summary>
    Task<int> AtomicIncrementReportCountAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically decrements the report count using a single SQL UPDATE.
    /// Prevents negative values by applying a server-side floor of 0.
    /// Returns the number of rows affected (0 if profile not found).
    /// </summary>
    Task<int> AtomicDecrementReportCountAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns Tier-0 creators eligible for promotion to Tier-1.
    /// Criteria: ArticleCount >= 5, ReportRate &lt; 5%, not already flagged EligibleForTier1.
    /// </summary>
    Task<List<CreatorProfile>> GetTier0PromotionCandidatesAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns Tier-1 creators eligible for promotion to Tier-2.
    /// Criteria: ArticleCount >= 25, ReportRate &lt; 2%, TotalReactionCount >= 500, not already flagged EligibleForTier2.
    /// </summary>
    Task<List<CreatorProfile>> GetTier1PromotionCandidatesAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns creators whose ReportRate exceeds the auto-demotion threshold (15%)
    /// for at least the specified sustained period.
    /// </summary>
    Task<List<CreatorProfile>> GetAutoDemotionCandidatesAsync(
        double reportRateThreshold,
        int batchSize,
        CancellationToken cancellationToken = default);
}
