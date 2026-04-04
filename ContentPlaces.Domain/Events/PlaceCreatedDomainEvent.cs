using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Domain.Events;

public sealed record PlaceCreatedDomainEvent(
    Guid PlaceId,
    string Name,
    string Slug) : DomainEventBase;
