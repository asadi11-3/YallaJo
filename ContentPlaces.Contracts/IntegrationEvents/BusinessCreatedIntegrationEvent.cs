using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessCreatedIntegrationEvent(
    Guid BusinessId,
    string Name,
    string Slug,
    Guid OwnerId,
    Guid? PlaceId) : IntegrationEventBase;
