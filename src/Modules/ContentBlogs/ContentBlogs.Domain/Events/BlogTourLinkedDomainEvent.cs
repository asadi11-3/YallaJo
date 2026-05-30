using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogTourLinkedDomainEvent(
    Guid BlogId,
    Guid TourId,
    DateTime LinkedAtUtc) : DomainEventBase;
