using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessRejectedIntegrationEvent(
    Guid BusinessId,
    Guid OwnerId,
    string Reason) : IntegrationEventBase;
