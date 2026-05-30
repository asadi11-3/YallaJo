using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record PlaceDeletedIntegrationEvent(Guid PlaceId) : IntegrationEventBase;
