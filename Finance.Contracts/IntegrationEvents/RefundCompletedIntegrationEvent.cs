using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.refund.completed.v1</c>) when a refund has been
/// confirmed by the gateway. Consumers: Booking (final cancellation accounting),
/// Messaging (user confirmation), Analytics.
/// </summary>
public sealed record RefundCompletedIntegrationEvent(
    Guid RefundPaymentId,
    Guid OriginalPaymentId,
    Guid BookingId,
    decimal Amount,
    string Currency,
    string Reason,
    string GatewayRefundId,
    DateTime CompletedAt) : IntegrationEventBase;
