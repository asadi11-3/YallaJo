using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events.Agency;

public sealed record AgencyApplicationApprovedDomainEvent(
    Guid ApplicationId,
    Guid GuideUserId,
    Guid AgencyUserId) : DomainEventBase;
