using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.commission-rule.deleted.v1</c>) when a
/// commission rule has been soft-deleted. Consumers: Analytics, cache invalidation.
/// </summary>
public sealed record CommissionRuleDeletedIntegrationEvent(
    Guid RuleId,
    DateTime OccurredAt) : IntegrationEventBase;
