using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourGuideUpdatedDomainEvent(Guid TourGuideId, Guid UserId) : DomainEventBase;
