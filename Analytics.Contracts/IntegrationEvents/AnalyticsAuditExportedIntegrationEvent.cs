using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Contracts.IntegrationEvents;

public sealed record AnalyticsAuditExportedIntegrationEvent(
    Guid ExportId,
    Guid RequestedByUserId,
    string ExportType,
    int RecordCount,
    DateTime ExportedAt) : IntegrationEventBase;
