using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

/// <summary>
/// Tracks impression → click → booking funnel for recommendation quality measurement.
/// </summary>
public sealed class SuggestionMetric : BaseEntity<long>
{
    private SuggestionMetric() { }

    public Guid? BatchId { get; private set; }
    public Guid? RecommendationCacheId { get; private set; }
    public Guid? UserId { get; private set; }
    public int Position { get; private set; }
    public string Stage { get; private set; } = string.Empty; // Impression, Click, Booking
    public DateTime OccurredAt { get; private set; }
    public string? SessionId { get; private set; }
    public string? ExperimentVariant { get; private set; }

    public static SuggestionMetric Record(
        Guid? batchId, Guid? recommendationCacheId, Guid? userId,
        int position, string stage, string? sessionId, string? experimentVariant)
        => new()
        {
            BatchId = batchId,
            RecommendationCacheId = recommendationCacheId,
            UserId = userId,
            Position = position,
            Stage = stage,
            OccurredAt = DateTime.UtcNow,
            SessionId = sessionId,
            ExperimentVariant = experimentVariant
        };

    public void AnonymizeUser() => UserId = null;
}
