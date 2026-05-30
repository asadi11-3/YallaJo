using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when a refund has been initiated for an existing completed payment.
/// A refund is modelled as a sibling <c>Payment</c> row with <see cref="Finance.Domain.Enums.PaymentType"/>=Refund
/// and <c>OriginalPaymentId</c> pointing back to the captured payment.
/// </summary>
/// <param name="RefundPaymentId">Identifier of the refund payment row.</param>
/// <param name="OriginalPaymentId">Identifier of the captured payment being refunded.</param>
/// <param name="BookingId">Identifier of the tour booking.</param>
/// <param name="Amount">Refund amount (always positive; the row stores it as negative).</param>
/// <param name="Currency">3-letter ISO-4217 currency code (must equal the original payment currency).</param>
/// <param name="Reason">Refund reason code (see <see cref="Finance.Domain.Enums.RefundReason"/>).</param>
public sealed record RefundInitiatedDomainEvent(
    Guid RefundPaymentId,
    Guid OriginalPaymentId,
    Guid BookingId,
    decimal Amount,
    string Currency,
    string Reason) : DomainEventBase;
