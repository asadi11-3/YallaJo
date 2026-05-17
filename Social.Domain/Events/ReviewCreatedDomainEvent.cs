using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record ReviewCreatedDomainEvent(
    Guid ReviewId,
    Guid UserId,
    Guid? PlaceId,
    Guid? TourId,
    Guid? TourGuideId,
    Guid? BusinessId,
    decimal Rating,
    bool IsVerified) : DomainEventBase;
