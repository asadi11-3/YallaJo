using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when the gateway confirms the refund has settled successfully.
/// </summary>
/// <param name="RefundPaymentId">Identifier of the refund payment row.</param>
/// <param name="OriginalPaymentId">Identifier of the captured payment that was refunded.</param>
/// <param name="BookingId">Identifier of the tour booking.</param>
/// <param name="Amount">Refunded amount (always positive).</param>
/// <param name="Currency">3-letter ISO-4217 currency code.</param>
/// <param name="Reason">Refund reason code.</param>
/// <param name="GatewayRefundId">Authoritative gateway refund identifier.</param>
public sealed record RefundCompletedDomainEvent(
    Guid RefundPaymentId,
    Guid OriginalPaymentId,
    Guid BookingId,
    decimal Amount,
    string Currency,
    string Reason,
    string GatewayRefundId) : DomainEventBase;
