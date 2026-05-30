using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Domain.Events.BusinessEvents;

public sealed record BusinessDeletedDomainEvent(Guid BusinessId) : DomainEventBase;
