using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events.Agency;

public sealed record AgencyAffiliationTerminatedDomainEvent(
    Guid AffiliationId,
    Guid AgencyUserId,
    Guid GuideUserId,
    Guid TerminatedByUserId,
    string Reason) : DomainEventBase;
