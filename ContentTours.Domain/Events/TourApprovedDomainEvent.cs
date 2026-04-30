using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourApprovedDomainEvent(
    Guid TourId,
    Guid CreatedByUserId,
    Guid ApprovedByUserId,
    DateTime ApprovedAt) : DomainEventBase;
