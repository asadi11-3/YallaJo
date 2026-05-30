namespace Social.Contracts.Services;

public interface INsfwClassifier
{
    Task<NsfwScoreResult> ClassifyAsync(string imageUrl, CancellationToken ct = default);
}

public sealed record NsfwScoreResult(decimal Score, bool IsExplicit);
