using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourSubmittedDomainEvent(
    Guid TourId,
    Guid CreatedByUserId,
    DateTime SubmittedAt) : DomainEventBase;
