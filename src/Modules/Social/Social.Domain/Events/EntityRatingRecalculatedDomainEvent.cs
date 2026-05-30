using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when the Bayesian rating is recalculated for an entity (daily 03:00 UTC).</summary>
public sealed record EntityRatingRecalculatedDomainEvent(
    Social.Domain.Enums.ReviewTargetType TargetType, Guid TargetId,
    decimal NewAverageRating, int NewReviewCount, DateTime RecalculatedAt) : DomainEventBase;
