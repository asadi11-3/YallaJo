using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Domain.Events;

public sealed record PlaceUpdatedDomainEvent(
    Guid PlaceId,
    string Name,
    string? Description,
    string? Address) : DomainEventBase;
