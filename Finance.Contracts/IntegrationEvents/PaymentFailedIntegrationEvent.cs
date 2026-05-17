using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

public sealed record PaymentFailedIntegrationEvent(
    Guid PaymentId,
    Guid UserId,
    Guid? BookingId,
    string Reason) : IntegrationEventBase;
