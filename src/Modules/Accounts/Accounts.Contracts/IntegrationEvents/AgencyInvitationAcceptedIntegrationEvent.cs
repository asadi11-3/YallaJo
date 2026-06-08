using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

public sealed record AgencyInvitationAcceptedIntegrationEvent(
    Guid InvitationId,
    Guid AgencyUserId,
    Guid GuideUserId,
    DateTime AcceptedAt) : IntegrationEventBase;
