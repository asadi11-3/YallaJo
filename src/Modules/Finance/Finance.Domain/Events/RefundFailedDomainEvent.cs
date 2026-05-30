using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when the gateway reports a failure for a previously initiated refund.
/// The <see cref="AttemptCount"/> drives the retry policy in <c>RefundRetryService</c>.
/// </summary>
/// <param name="RefundPaymentId">Identifier of the refund payment row.</param>
/// <param name="OriginalPaymentId">Identifier of the captured payment being refunded.</param>
/// <param name="BookingId">Identifier of the tour booking.</param>
/// <param name="Amount">Refund amount that failed.</param>
/// <param name="Currency">3-letter ISO-4217 currency code.</param>
/// <param name="Reason">Original refund reason code.</param>
/// <param name="AttemptCount">Number of attempts so far (1-based).</param>
/// <param name="FailureReason">Free-form failure reason from the gateway.</param>
public sealed record RefundFailedDomainEvent(
    Guid RefundPaymentId,
    Guid OriginalPaymentId,
    Guid BookingId,
    decimal Amount,
    string Currency,
    string Reason,
    int AttemptCount,
    string FailureReason) : DomainEventBase;
