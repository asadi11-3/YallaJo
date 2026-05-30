using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when a per-booking line item is added to a payout batch.
/// </summary>
/// <param name="PayoutId">Identifier of the parent payout batch.</param>
/// <param name="BookingId">Identifier of the booking being accounted for.</param>
/// <param name="GrossAmount">Gross amount captured for the booking.</param>
/// <param name="CommissionAmount">Platform commission deducted from the booking.</param>
/// <param name="NetAmount">Net amount payable to the provider for this booking.</param>
/// <param name="Currency">3-letter ISO-4217 currency code.</param>
public sealed record PayoutItemAddedDomainEvent(
    Guid PayoutId,
    Guid BookingId,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency) : DomainEventBase;
