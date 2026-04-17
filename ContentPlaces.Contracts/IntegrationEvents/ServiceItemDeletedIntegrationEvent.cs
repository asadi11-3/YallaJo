using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record ServiceItemDeletedIntegrationEvent(
    Guid ServiceItemId,
    Guid BusinessId) : IntegrationEventBase;
