using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.payment.completed.v1</c>) when a payment has been
/// successfully completed by the gateway. Consumers: Booking (confirms booking),
/// Invoice generation (T3), Analytics.
/// </summary>
public sealed record PaymentCompletedIntegrationEvent(
    Guid PaymentId,
    Guid BookingId,
    Guid UserId,
    Guid ProviderId,
    decimal Amount,
    string Currency,
    string GatewayTransactionId,
    DateTime CompletedAt) : IntegrationEventBase;
