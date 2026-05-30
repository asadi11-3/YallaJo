using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogFeaturedDomainEvent(
    Guid BlogId,
    string Slug,
    string Title,
    Guid AuthorId,
    Guid? PlaceId,
    DateTime FeaturedAtUtc) : DomainEventBase;
