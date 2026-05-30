using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record ServiceItemCreatedIntegrationEvent(
    Guid ServiceItemId,
    Guid BusinessId,
    string Name,
    decimal Price,
    string Currency) : IntegrationEventBase;
