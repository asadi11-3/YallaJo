using Social.Domain.Enums;

namespace Social.Presentation.Endpoints.Report.Models;

internal sealed record ResolveReportRequest(
    ModerationAction Action,
    string? Notes = null);
