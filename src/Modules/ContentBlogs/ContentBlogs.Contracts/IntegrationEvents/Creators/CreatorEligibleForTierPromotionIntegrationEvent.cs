using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorEligibleForTierPromotionIntegrationEvent(
    Guid CreatorProfileId,
    Guid UserId,
    string CurrentTier,
    string EligibleForTier,
    DateTime DetectedAtUtc) : IntegrationEventBase;
