using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

public sealed record PaymentRefundedIntegrationEvent(
    Guid PaymentId,
    Guid UserId,
    Guid? BookingId,
    decimal RefundedAmount,
    string Currency,
    string Reason) : IntegrationEventBase;
