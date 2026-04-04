using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record PlaceCreatedIntegrationEvent(
    Guid PlaceId,
    string Name,
    string Slug) : IntegrationEventBase;
