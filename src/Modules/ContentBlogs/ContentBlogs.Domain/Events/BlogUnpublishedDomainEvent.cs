using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogUnpublishedDomainEvent(
    Guid BlogId,
    string Slug,
    DateTime UnpublishedAtUtc) : DomainEventBase;
