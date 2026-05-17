using Social.Contracts.Services;

namespace Social.Infrastructure.Services;

internal sealed class NoopNsfwClassifier : INsfwClassifier
{
    public Task<NsfwScoreResult> ClassifyAsync(string imageUrl, CancellationToken ct = default)
        => Task.FromResult(new NsfwScoreResult(Score: 0m, IsExplicit: false));
}
