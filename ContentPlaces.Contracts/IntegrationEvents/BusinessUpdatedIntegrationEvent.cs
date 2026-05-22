using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessUpdatedIntegrationEvent(
    Guid BusinessId,
    string Name,
    string? Slug,
    Guid? PlaceId,
    bool? IsHalal,
    bool? HasVegetarianOptions,
    bool? HasAlcoholFreeArea) : IntegrationEventBase;
