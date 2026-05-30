using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideRegisteredIntegrationEvent(
    Guid TourGuideId,
    Guid UserId
) : IntegrationEventBase;
