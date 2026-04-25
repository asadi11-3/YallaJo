using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record CategoryRestoredDomainEvent(
    Guid CategoryId,
    string Slug) : DomainEventBase;
