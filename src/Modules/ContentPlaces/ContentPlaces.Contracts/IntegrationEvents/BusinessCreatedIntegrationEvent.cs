using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessCreatedIntegrationEvent(
    Guid BusinessId,
    string Name,
    string Slug,
    Guid OwnerId,
    Guid? PlaceId,
    bool? IsHalal,
    bool? HasVegetarianOptions,
    bool? HasAlcoholFreeArea) : IntegrationEventBase;
