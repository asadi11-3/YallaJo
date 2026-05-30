using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when a payout batch row is created for a provider for a given batch period.
/// </summary>
/// <param name="PayoutBatchId">Identifier of the new payout aggregate.</param>
/// <param name="BatchPeriodStart">Inclusive start of the batch period.</param>
/// <param name="BatchPeriodEnd">Inclusive end of the batch period.</param>
/// <param name="TotalAmount">Gross total before commission deduction.</param>
/// <param name="Currency">3-letter ISO-4217 currency code.</param>
/// <param name="ItemCount">Number of <c>PayoutItem</c> rows aggregated under the batch.</param>
public sealed record PayoutBatchCreatedDomainEvent(
    Guid PayoutBatchId,
    DateOnly BatchPeriodStart,
    DateOnly BatchPeriodEnd,
    decimal TotalAmount,
    string Currency,
    int ItemCount) : DomainEventBase;
