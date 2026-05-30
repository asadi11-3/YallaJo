using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourUpdatedIntegrationEvent(
    Guid TourId,
    bool NameChanged,
    bool DescriptionChanged,
    bool ShortDescriptionChanged,
    bool PlaceIdChanged,
    bool ChildrenInfoChanged) : IntegrationEventBase;
