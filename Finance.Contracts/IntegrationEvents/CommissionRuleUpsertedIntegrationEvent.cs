using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.commission-rule.upserted.v1</c>) when a
/// commission rule is created or updated. Consumers: Analytics (commission audit
/// trail), cache invalidation listeners.
/// </summary>
public sealed record CommissionRuleUpsertedIntegrationEvent(
    Guid RuleId,
    string Tier,
    decimal MinMonthlyRevenue,
    decimal? MaxMonthlyRevenue,
    string Currency,
    decimal Percentage,
    DateTime OccurredAt) : IntegrationEventBase;
