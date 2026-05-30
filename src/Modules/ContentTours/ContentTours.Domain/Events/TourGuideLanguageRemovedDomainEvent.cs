using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourGuideLanguageRemovedDomainEvent(
    Guid TourGuideId,
    Guid UserId,
    Guid LanguageId) : DomainEventBase;
