using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

public sealed record AgencyAffiliationCreatedIntegrationEvent(
    Guid AffiliationId,
    Guid AgencyUserId,
    Guid GuideUserId,
    decimal CommissionPercentage,
    DateTime CreatedAt) : IntegrationEventBase;
