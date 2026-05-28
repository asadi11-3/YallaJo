using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideActivatedIntegrationEvent(
    Guid TourGuideId,
    Guid UserId,
    string DisplayName,
    string Slug,
    DateTime ActivatedAtUtc) : IntegrationEventBase;
