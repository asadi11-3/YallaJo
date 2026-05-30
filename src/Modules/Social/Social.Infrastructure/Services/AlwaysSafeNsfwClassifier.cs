using Social.Contracts.Services;

namespace Social.Infrastructure.Services;

/// <summary>
/// Stub NSFW classifier that always returns a safe (non-explicit) result.
/// Used in dev/test environments. Replace with a real ML classifier in production.
/// </summary>
internal sealed class AlwaysSafeNsfwClassifier : INsfwClassifier
{
    public Task<NsfwScoreResult> ClassifyAsync(string imageUrl, CancellationToken ct = default)
        => Task.FromResult(new NsfwScoreResult(Score: 0m, IsExplicit: false));
}
