using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogUnfeaturedDomainEvent(
    Guid BlogId,
    string Slug,
    Guid? PlaceId,
    DateTime UnfeaturedAtUtc) : DomainEventBase;
