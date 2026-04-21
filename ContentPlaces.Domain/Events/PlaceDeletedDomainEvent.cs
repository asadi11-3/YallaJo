using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Domain.Events;

public sealed record PlaceDeletedDomainEvent(Guid PlaceId) : DomainEventBase;
