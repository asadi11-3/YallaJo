using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

public sealed record AgencyAffiliationTerminatedIntegrationEvent(
    Guid AffiliationId,
    Guid AgencyUserId,
    Guid GuideUserId,
    Guid TerminatedByUserId,
    string Reason,
    DateTime TerminatedAt) : IntegrationEventBase;
