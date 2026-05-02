using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideUnassignedIntegrationEvent(
    Guid TourId,
    Guid TourGuideUserId,
    Guid UnassignedByUserId
) : IntegrationEventBase;
