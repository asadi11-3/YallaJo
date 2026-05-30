using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Domain.Events.BusinessEvents;

public sealed record BusinessResubmittedDomainEvent(
    Guid BusinessId) : DomainEventBase;
