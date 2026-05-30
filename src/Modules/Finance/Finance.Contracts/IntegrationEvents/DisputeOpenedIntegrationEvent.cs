using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

public sealed record DisputeOpenedIntegrationEvent(
    Guid DisputeId,
    Guid PaymentId,
    Guid UserId,
    string Reason) : IntegrationEventBase;
