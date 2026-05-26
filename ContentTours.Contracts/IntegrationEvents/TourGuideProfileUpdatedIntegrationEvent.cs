using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourGuideProfileUpdatedIntegrationEvent(
    Guid TourGuideId,
    Guid UserId,
    string? OldSlug,
    string NewSlug,
    DateTime UpdatedAtUtc) : IntegrationEventBase;
