using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourDeletedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    Guid? PlaceId,
    DateTime DeletedAt
) : IntegrationEventBase;
