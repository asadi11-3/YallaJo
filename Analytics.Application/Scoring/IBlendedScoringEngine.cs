using Analytics.Domain.Entities;
using Analytics.Domain.Enums;

namespace Analytics.Application.Scoring;

/// <summary>
/// Blended scoring engine that combines content-based and collaborative scores
/// using configurable weights (e.g., 60% content + 40% collaborative).
/// Implementation deferred — depends on ICollaborativeScoringEngine.
/// </summary>
public interface IBlendedScoringEngine
{
    /// <summary>
    /// Blends content-based scores with collaborative scores for the given candidates.
    /// Falls back to content-only scoring when collaborative data is unavailable.
    /// </summary>
    Task<IReadOnlyList<ScoredCandidate>> BlendAsync(
        Guid userId,
        ScoringContext context,
        IReadOnlyList<ScoredCandidate> contentScored,
        CancellationToken ct = default);
}

/// <summary>
/// NoOp implementation — passes through content-based scores unchanged until blended scoring is built.
/// </summary>
public sealed class NoOpBlendedScoringEngine : IBlendedScoringEngine
{
    public Task<IReadOnlyList<ScoredCandidate>> BlendAsync(
        Guid userId,
        ScoringContext context,
        IReadOnlyList<ScoredCandidate> contentScored,
        CancellationToken ct = default)
        => Task.FromResult(contentScored);
}
