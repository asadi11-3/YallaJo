using Social.Domain.Enums;
using Social.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>
/// Cached Bayesian rating for a reviewable entity (tour, place, business, tour-guide).
/// Promoted to IAggregateRoot so EntityRatingRecalculatedDomainEvent flows to outbox
/// as a social.rating.recalculated.v1 integration event.
/// Minimum 3 reviews before a BayesianScore is surfaced (S-R6).
/// </summary>
public sealed class EntityRatingCache : AuditableEntity, IAggregateRoot
{
    private EntityRatingCache() { } // EF Core

    public ReviewTargetType TargetType      { get; private set; }
    public Guid TargetId                    { get; private set; }

    /// <summary>Simple arithmetic mean of all published, non-deleted reviews.</summary>
    public decimal AverageRating            { get; private set; }

    /// <summary>Count of reviews included in the current calculation.</summary>
    public int ReviewCount                  { get; private set; }

    /// <summary>
    /// Bayesian smoothed score: (N * avg + C * globalAvg) / (N + C), C = 10.
    /// Suppressed from public display until ReviewCount >= 3.
    /// </summary>
    public decimal BayesianScore            { get; private set; }

    public DateTime LastRecalculatedAt      { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static EntityRatingCache Create(
        ReviewTargetType targetType,
        Guid targetId,
        decimal averageRating,
        int reviewCount,
        decimal bayesianScore,
        DateTime recalculatedAt)
    {
        var cache = new EntityRatingCache
        {
            TargetType         = targetType,
            TargetId           = targetId,
            AverageRating      = averageRating,
            ReviewCount        = reviewCount,
            BayesianScore      = bayesianScore,
            LastRecalculatedAt = recalculatedAt,
        };
        cache.AddDomainEvent(new EntityRatingRecalculatedDomainEvent(
            targetType, targetId, averageRating, reviewCount, recalculatedAt));
        return cache;
    }

    // ── State transition ──────────────────────────────────────────────────────

    public void Update(
        decimal averageRating,
        int reviewCount,
        decimal bayesianScore,
        DateTime recalculatedAt)
    {
        AverageRating      = averageRating;
        ReviewCount        = reviewCount;
        BayesianScore      = bayesianScore;
        LastRecalculatedAt = recalculatedAt;
        MarkUpdated();
        AddDomainEvent(new EntityRatingRecalculatedDomainEvent(
            TargetType, TargetId, averageRating, reviewCount, recalculatedAt));
    }
}
