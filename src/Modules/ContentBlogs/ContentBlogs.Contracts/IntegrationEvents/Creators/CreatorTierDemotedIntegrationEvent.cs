using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorTierDemotedIntegrationEvent(
    Guid CreatorProfileId,
    Guid UserId,
    string PreviousTier,
    string NewTier,
    string Reason,
    DateTime DemotedAtUtc) : IntegrationEventBase;
