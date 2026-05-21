using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourGuideRegisteredDomainEvent(Guid TourGuideId, Guid UserId) : DomainEventBase;
