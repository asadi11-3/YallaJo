using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorInvitationRedeemedIntegrationEvent(
    Guid InvitationId,
    Guid RedeemedByUserId,
    DateTime RedeemedAtUtc) : IntegrationEventBase;
