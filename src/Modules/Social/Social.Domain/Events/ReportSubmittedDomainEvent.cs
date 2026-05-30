using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

/// <summary>Raised when a user submits a report against any reportable entity.</summary>
public sealed record ReportSubmittedDomainEvent(
    Guid ReportId, Guid ReporterUserId,
    Social.Domain.Enums.ReportableEntityType EntityType, Guid EntityId,
    Social.Domain.Enums.ReportReason Reason, DateTime SubmittedAt) : DomainEventBase;
