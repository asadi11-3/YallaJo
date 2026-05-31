using Social.Domain.Enums;

namespace Social.Presentation.Endpoints.Report.Models;

internal sealed record SubmitReportRequest(
    ReportableEntityType EntityType,
    Guid EntityId,
    ReportReason Reason,
    string Description);
