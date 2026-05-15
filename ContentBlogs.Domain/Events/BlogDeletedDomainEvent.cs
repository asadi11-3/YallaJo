using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogDeletedDomainEvent(
    Guid BlogId,
    string Slug,
    DateTime DeletedAtUtc) : DomainEventBase;
