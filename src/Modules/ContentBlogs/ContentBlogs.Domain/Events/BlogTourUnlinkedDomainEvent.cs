using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogTourUnlinkedDomainEvent(
    Guid BlogId,
    Guid TourId,
    DateTime UnlinkedAtUtc) : DomainEventBase;
