using Analytics.Domain.Entities;
using Analytics.Domain.Enums;

namespace Analytics.Application.Scoring;

/// <summary>
/// Collaborative filtering engine that scores candidates based on user-user or item-item similarity.
/// Implementation deferred — requires nightly matrix recomputation service.
/// </summary>
public interface ICollaborativeScoringEngine
{
    /// <summary>
    /// Returns collaborative scores for the given candidates, keyed by (EntityKind, EntityId).
    /// Returns empty dictionary if no collaborative data is available for the user.
    /// </summary>
    Task<IReadOnlyDictionary<(EntityType Kind, Guid Id), decimal>> GetScoresAsync(
        Guid userId,
        IReadOnlyList<EntityAttributeSnapshot> candidates,
        CancellationToken ct = default);
}

/// <summary>
/// NoOp implementation — returns empty scores until collaborative filtering is built.
/// </summary>
public sealed class NoOpCollaborativeScoringEngine : ICollaborativeScoringEngine
{
    public Task<IReadOnlyDictionary<(EntityType Kind, Guid Id), decimal>> GetScoresAsync(
        Guid userId,
        IReadOnlyList<EntityAttributeSnapshot> candidates,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<(EntityType Kind, Guid Id), decimal>>(
            new Dictionary<(EntityType Kind, Guid Id), decimal>());
}
