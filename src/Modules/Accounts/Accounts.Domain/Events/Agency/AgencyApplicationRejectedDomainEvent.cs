using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events.Agency;

public sealed record AgencyApplicationRejectedDomainEvent(
    Guid ApplicationId,
    Guid GuideUserId,
    Guid AgencyUserId,
    string Reason) : DomainEventBase;
