using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record BulkAnalyticsExportedDomainEvent(
    Guid ExportId,
    Guid RequestedByUserId,
    string ExportType,
    int RecordCount) : DomainEventBase;
