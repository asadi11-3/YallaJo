using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record GuideApplicationApprovedDomainEvent(
    Guid ApplicationId,
    Guid TourId,
    Guid GuideUserId,
    Guid ApprovedByAdminId) : DomainEventBase;
