using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a background service determines a creator meets promotion criteria.</summary>
public sealed record CreatorEligibleForTierPromotionDomainEvent(
    Guid CreatorProfileId,
    Guid UserId,
    CreatorTrustTier CurrentTier,
    CreatorTrustTier EligibleForTier) : DomainEventBase;
