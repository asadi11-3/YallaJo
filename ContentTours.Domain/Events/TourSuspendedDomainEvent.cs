using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourSuspendedDomainEvent(
    Guid TourId,
    Guid CreatedByUserId,
    string Reason,
    DateTime SuspendedAt) : DomainEventBase;
