using Social.Domain.Enums;

namespace Social.Presentation.Endpoints.Review.Models;

internal sealed record ReviewReportRequest(ReportReason Reason, string Description);
