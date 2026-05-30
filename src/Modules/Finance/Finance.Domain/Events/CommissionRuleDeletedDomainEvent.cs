using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when a commission rule has been soft-deleted.
/// </summary>
/// <param name="RuleId">Identifier of the soft-deleted commission rule.</param>
public sealed record CommissionRuleDeletedDomainEvent(
    Guid RuleId) : DomainEventBase;
