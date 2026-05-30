using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideUpdatedIntegrationEvent(
    Guid TourGuideId,
    Guid UserId
) : IntegrationEventBase;
