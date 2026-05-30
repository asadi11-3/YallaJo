using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogApprovedDomainEvent(
    Guid BlogId,
    string Slug,
    Guid ApprovedByAdminId,
    Guid? AuthoredByCreatorId,
    DateTime ApprovedAtUtc) : DomainEventBase;
