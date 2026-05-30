using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record GuideApplicationRejectedDomainEvent(
    Guid ApplicationId,
    Guid TourId,
    Guid GuideUserId,
    string Reason) : DomainEventBase;
