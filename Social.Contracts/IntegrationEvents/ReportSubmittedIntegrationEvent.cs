using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

/// <summary>social.report.submitted.v1 — emitted when a user submits an abuse report.</summary>
public sealed record ReportSubmittedIntegrationEvent(
    Guid ReportId, Guid ReporterUserId, string EntityType, Guid EntityId,
    string Reason, DateTime SubmittedAt) : IntegrationEventBase;
