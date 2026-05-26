using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideDeactivatedIntegrationEvent(
    Guid TourGuideId,
    Guid UserId,
    DateTime DeactivatedAtUtc) : IntegrationEventBase;
