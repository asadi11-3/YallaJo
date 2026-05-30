using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogArchivedDomainEvent(
    Guid BlogId,
    string Slug,
    DateTime ArchivedAtUtc) : DomainEventBase;
