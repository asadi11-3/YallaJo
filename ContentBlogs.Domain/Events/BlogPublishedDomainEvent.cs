using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogPublishedDomainEvent(
    Guid BlogId,
    string Slug,
    string Title,
    Guid AuthorId,
    Guid? PlaceId,
    DateTime PublishedAtUtc) : DomainEventBase;
