using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourRejectedDomainEvent(
    Guid TourId,
    Guid CreatedByUserId,
    Guid RejectedByUserId,
    string Reason,
    DateTime RejectedAt) : DomainEventBase;
