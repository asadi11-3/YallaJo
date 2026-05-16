using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogRestoredDomainEvent(
    Guid BlogId,
    string Slug,
    DateTime RestoredAtUtc) : DomainEventBase;
