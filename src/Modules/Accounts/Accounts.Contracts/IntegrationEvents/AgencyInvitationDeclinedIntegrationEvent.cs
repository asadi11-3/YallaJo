using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

public sealed record AgencyInvitationDeclinedIntegrationEvent(
    Guid InvitationId,
    Guid AgencyUserId,
    Guid GuideUserId,
    DateTime DeclinedAt) : IntegrationEventBase;
