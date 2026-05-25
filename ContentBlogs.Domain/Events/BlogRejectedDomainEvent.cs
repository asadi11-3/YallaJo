using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogRejectedDomainEvent(
    Guid BlogId,
    string Slug,
    Guid RejectedByAdminId,
    string Reason,
    Guid? AuthoredByCreatorId,
    DateTime RejectedAtUtc) : DomainEventBase;
