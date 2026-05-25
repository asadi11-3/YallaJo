using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events.Agency;

public sealed record AgencyAffiliationCreatedDomainEvent(
    Guid AffiliationId,
    Guid AgencyUserId,
    Guid GuideUserId) : DomainEventBase;
