namespace YallaJo.Web.Areas.Admin.Models.Reports;

public static class ReportsMapper
{
    public static ReportsVm ToVm(ReportPageResponse page, IReadOnlyDictionary<Guid, string>? reporterEmails = null)
    {
        return new ReportsVm
        {
            NextCursor = page.NextCursor,
            Reports = page.Items.Select(r => new ReportRowVm
            {
                Id = r.Id,
                ReporterUserId = r.ReporterUserId,
                ReporterEmail = reporterEmails is not null && reporterEmails.TryGetValue(r.ReporterUserId, out var email) ? email : null,
                EntityType = r.EntityType,
                EntityId = r.EntityId,
                Reason = r.Reason,
                Description = r.Description,
                Status = r.Status,
                SubmittedAt = r.SubmittedAt,
                ResolutionAction = r.ResolutionAction,
                ResolutionNotes = r.ResolutionNotes,
                CanResolve = string.Equals(r.Status, "Open", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(r.Status, "UnderReview", StringComparison.OrdinalIgnoreCase),
            }).ToList(),
        };
    }

    public static string StatusColor(string status) => status switch
    {
        _ when string.Equals(status, "Resolved", StringComparison.OrdinalIgnoreCase) => "success",
        _ when string.Equals(status, "UnderReview", StringComparison.OrdinalIgnoreCase) => "info",
        _ when string.Equals(status, "Open", StringComparison.OrdinalIgnoreCase) => "warning",
        _ when string.Equals(status, "Dismissed", StringComparison.OrdinalIgnoreCase) => "secondary",
        _ => "secondary",
    };

    public static string StatusIcon(string status) => status switch
    {
        _ when string.Equals(status, "Resolved", StringComparison.OrdinalIgnoreCase) => "circle-check",
        _ when string.Equals(status, "UnderReview", StringComparison.OrdinalIgnoreCase) => "magnifying-glass",
        _ when string.Equals(status, "Open", StringComparison.OrdinalIgnoreCase) => "clock",
        _ when string.Equals(status, "Dismissed", StringComparison.OrdinalIgnoreCase) => "circle-xmark",
        _ => "circle-info",
    };
}
