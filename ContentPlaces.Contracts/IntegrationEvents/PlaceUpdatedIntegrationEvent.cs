using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record PlaceUpdatedIntegrationEvent(
    Guid PlaceId,
    string Name,
    string? Description,
    string? Address) : IntegrationEventBase;
