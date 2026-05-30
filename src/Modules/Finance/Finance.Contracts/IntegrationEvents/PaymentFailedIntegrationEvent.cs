using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.payment.failed.v1</c>) when a payment attempt
/// has failed at the gateway. Consumers: Booking (releases slot lock and restores
/// capacity), Messaging (user notification), Analytics.
/// </summary>
public sealed record PaymentFailedIntegrationEvent(
    Guid PaymentId,
    Guid BookingId,
    string ReasonCode,
    string RawReason,
    DateTime OccurredAt) : IntegrationEventBase;
