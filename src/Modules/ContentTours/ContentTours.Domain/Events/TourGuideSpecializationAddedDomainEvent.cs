using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourGuideSpecializationAddedDomainEvent(
    Guid TourGuideId,
    Guid UserId,
    Guid SpecializationId) : DomainEventBase;
