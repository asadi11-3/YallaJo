using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorTierPromotedIntegrationEvent(
    Guid CreatorProfileId,
    Guid UserId,
    Guid PromotedByAdminId,
    string PreviousTier,
    string NewTier,
    DateTime PromotedAtUtc) : IntegrationEventBase;
