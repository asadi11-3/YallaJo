using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events.Agency;

public sealed record AgencyInvitationAcceptedDomainEvent(
    Guid InvitationId,
    Guid AgencyUserId,
    Guid GuideUserId) : DomainEventBase;
