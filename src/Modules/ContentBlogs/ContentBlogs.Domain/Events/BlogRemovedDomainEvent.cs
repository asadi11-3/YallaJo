using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogRemovedDomainEvent(
    Guid BlogId,
    string Slug,
    Guid RemovedByAdminId,
    string Reason,
    DateTime RemovedAtUtc) : DomainEventBase;
