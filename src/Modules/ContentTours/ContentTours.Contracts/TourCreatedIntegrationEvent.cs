using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourCreatedIntegrationEvent(
    Guid TourId,
    string Name,
    string Slug,
    Guid CreatedByUserId,
    Guid? PlaceId) : IntegrationEventBase;
