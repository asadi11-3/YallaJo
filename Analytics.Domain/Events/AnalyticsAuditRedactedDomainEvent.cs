using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record AnalyticsAuditRedactedDomainEvent(
    long AuditLogId,
    Guid RedactedByUserId,
    string Reason) : DomainEventBase;
