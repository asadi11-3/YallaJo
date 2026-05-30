using Analytics.Domain.Enums;
using Analytics.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class PopularityScore : AuditableEntity, IAggregateRoot
{
    private PopularityScore() { }

    public EntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public decimal Score { get; private set; }
    public int? TrendingRank { get; private set; }
    public DateTime? LastRecalculatedAt { get; private set; }
    public bool IsStale { get; private set; } = true;
    public int InteractionCountSnapshot { get; private set; }
    public decimal? AverageRatingSnapshot { get; private set; }
    public int ReviewCountSnapshot { get; private set; }
    public decimal? CategoryRankPercentile { get; private set; }

    public static PopularityScore Initialize(EntityType entityType, Guid entityId, DateTime now)
    {
        var score = new PopularityScore { EntityType = entityType, EntityId = entityId, Score = 0m, IsStale = true, InteractionCountSnapshot = 0 };
        score.AddDomainEvent(new PopularityScoreInitializedDomainEvent(score.Id, (int)entityType, entityId, now));
        return score;
    }

    public void Recalculate(decimal newScore, int interactionCount, DateTime now)
    {
        var oldScore = Score;
        Score = newScore;
        InteractionCountSnapshot = interactionCount;
        LastRecalculatedAt = now;
        IsStale = false;
        MarkUpdated();
        AddDomainEvent(new PopularityScoreRecalculatedDomainEvent(Id, (int)EntityType, EntityId, oldScore, newScore, interactionCount, now));
    }

    public void MarkStale(DateTime now)
    {
        if (IsStale) return;
        IsStale = true;
        MarkUpdated();
        AddDomainEvent(new PopularityScoreStaleFlaggedDomainEvent(Id, (int)EntityType, EntityId, now));
    }

    public void UpdateRatingSnapshot(decimal averageRating, int reviewCount, DateTime now)
    {
        AverageRatingSnapshot = averageRating;
        ReviewCountSnapshot = reviewCount;
        MarkStale(now);
    }

    public void SetTrendingRank(int? rank, DateTime now)
    {
        TrendingRank = rank;
        MarkUpdated();
    }

    public void SoftDelete(DateTime now)
    {
        SoftDelete();
        AddDomainEvent(new PopularityScoreSoftDeletedDomainEvent(Id, (int)EntityType, EntityId, now));
    }
}
