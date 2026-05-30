using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record GuideApplicationSubmittedDomainEvent(
    Guid ApplicationId,
    Guid TourId,
    Guid TourGuideId,
    Guid GuideUserId) : DomainEventBase;
