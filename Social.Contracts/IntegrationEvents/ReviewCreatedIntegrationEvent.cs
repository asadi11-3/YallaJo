using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

public sealed record ReviewCreatedIntegrationEvent(
    Guid ReviewId,
    Guid UserId,
    Guid? PlaceId,
    Guid? TourId,
    Guid? TourGuideId,
    Guid? BusinessId,
    decimal Rating,
    bool IsVerified) : IntegrationEventBase;
