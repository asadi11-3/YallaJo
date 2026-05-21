using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when a commission rule has been created or updated.
/// Emitted by both POST and PUT operations on the rule aggregate.
/// </summary>
/// <param name="RuleId">Identifier of the commission rule aggregate.</param>
/// <param name="Tier">Provider tier (e.g. <c>Free</c>, <c>Basic</c>, <c>Premium</c>, <c>Enterprise</c>).</param>
/// <param name="MinMonthlyRevenue">Inclusive lower bound of the monthly revenue range.</param>
/// <param name="MaxMonthlyRevenue">Exclusive upper bound of the monthly revenue range. Null = open-ended top tier.</param>
/// <param name="Currency">3-letter ISO-4217 currency code.</param>
/// <param name="Percentage">Commission percentage applied to the discounted price (0 &lt; p &lt; 100).</param>
public sealed record CommissionRuleUpsertedDomainEvent(
    Guid RuleId,
    string Tier,
    decimal MinMonthlyRevenue,
    decimal? MaxMonthlyRevenue,
    string Currency,
    decimal Percentage) : DomainEventBase;
