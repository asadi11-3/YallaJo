using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when the gateway reports a failure for a previously initiated payment.
/// Booking module reacts by releasing held capacity.
/// </summary>
/// <param name="PaymentId">Identifier of the failed payment aggregate.</param>
/// <param name="BookingId">Identifier of the tour booking the payment belongs to.</param>
/// <param name="ReasonCode">Stable machine-readable code (e.g. <c>card_declined</c>, <c>insufficient_funds</c>).</param>
/// <param name="RawReason">Original failure description from the gateway (free-form).</param>
/// <param name="OccurredAt">UTC instant when the gateway reported the failure.</param>
public sealed record PaymentFailedDomainEvent(
    Guid PaymentId,
    Guid BookingId,
    string ReasonCode,
    string RawReason,
    DateTime OccurredAt) : DomainEventBase;
