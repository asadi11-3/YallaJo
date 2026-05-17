using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

public sealed record PaymentSucceededIntegrationEvent(
    Guid PaymentId,
    Guid UserId,
    Guid? BookingId,
    decimal Amount,
    string Currency,
    string TransactionId,
    DateTime PaidAt) : IntegrationEventBase;
