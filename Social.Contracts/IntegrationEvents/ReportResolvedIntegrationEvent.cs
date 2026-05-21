using YallaJo.SharedKernel.Domain.Event;

namespace Social.Contracts.IntegrationEvents;

/// <summary>social.report.resolved.v1 — emitted when an admin resolves an abuse report.</summary>
public sealed record ReportResolvedIntegrationEvent(
    Guid ReportId, Guid AdminUserId, string EntityType, Guid EntityId,
    string Action, DateTime ResolvedAt) : IntegrationEventBase;
