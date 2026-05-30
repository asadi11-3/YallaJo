using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record CategoryDeletedDomainEvent(
    Guid CategoryId,
    string Slug) : DomainEventBase;
