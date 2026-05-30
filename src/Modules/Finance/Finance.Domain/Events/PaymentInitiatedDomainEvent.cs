using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when a payment has been initiated against the gateway for a booking.
/// </summary>
/// <param name="PaymentId">Identifier of the newly created payment aggregate.</param>
/// <param name="BookingId">Identifier of the tour booking the payment belongs to.</param>
/// <param name="UserId">Identifier of the user making the payment.</param>
/// <param name="ProviderId">Identifier of the tour provider receiving the funds (post escrow release).</param>
/// <param name="Amount">Amount initiated against the payment gateway.</param>
/// <param name="Currency">3-letter ISO-4217 currency code.</param>
public sealed record PaymentInitiatedDomainEvent(
    Guid PaymentId,
    Guid BookingId,
    Guid UserId,
    Guid ProviderId,
    decimal Amount,
    string Currency) : DomainEventBase;
