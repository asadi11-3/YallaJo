using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourGuideLanguageAddedDomainEvent(
    Guid TourGuideId,
    Guid UserId,
    Guid LanguageId,
    string Proficiency) : DomainEventBase;
