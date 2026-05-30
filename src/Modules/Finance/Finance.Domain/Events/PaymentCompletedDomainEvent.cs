using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when the gateway confirms the payment has captured funds successfully.
/// Triggers invoice generation and emits the <c>finance.payment.completed.v1</c> integration event.
/// </summary>
/// <param name="PaymentId">Identifier of the completed payment aggregate.</param>
/// <param name="BookingId">Identifier of the tour booking the payment belongs to.</param>
/// <param name="UserId">Identifier of the user that paid.</param>
/// <param name="ProviderId">Identifier of the tour provider receiving the funds (post escrow release).</param>
/// <param name="Amount">Captured amount.</param>
/// <param name="Currency">3-letter ISO-4217 currency code.</param>
/// <param name="GatewayTransactionId">Authoritative gateway transaction identifier (UNIQUE in the database).</param>
public sealed record PaymentCompletedDomainEvent(
    Guid PaymentId,
    Guid BookingId,
    Guid UserId,
    Guid ProviderId,
    decimal Amount,
    string Currency,
    string GatewayTransactionId) : DomainEventBase;
