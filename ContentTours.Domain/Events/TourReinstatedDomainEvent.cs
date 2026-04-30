using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourReinstatedDomainEvent(
    Guid TourId,
    Guid CreatedByUserId,
    DateTime ReinstatedAt) : DomainEventBase;
