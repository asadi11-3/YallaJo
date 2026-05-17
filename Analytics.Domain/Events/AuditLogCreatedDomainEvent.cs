using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record AuditLogCreatedDomainEvent(
    long AuditLogId,
    Guid? UserId,
    string Action,
    string EntityType,
    string? EntityId) : DomainEventBase;
