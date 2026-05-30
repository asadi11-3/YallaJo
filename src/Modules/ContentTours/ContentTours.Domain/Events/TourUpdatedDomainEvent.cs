using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourUpdatedDomainEvent(
    Guid TourId,
    bool NameChanged,
    bool DescriptionChanged,
    bool ShortDescriptionChanged,
    bool PlaceIdChanged,
    bool ChildrenInfoChanged) : DomainEventBase;
