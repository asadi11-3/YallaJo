namespace YallaJo.Web.Areas.Admin.Models.Disputes;

public static class DisputesMapper
{
    public static DisputesVm ToVm(IReadOnlyList<DisputeResponse> disputes, string? statusFilter)
    {
        IEnumerable<DisputeResponse> source = disputes;
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            source = source.Where(d => string.Equals(d.Status, statusFilter, StringComparison.OrdinalIgnoreCase));
        }

        return new DisputesVm
        {
            StatusFilter = statusFilter,
            Disputes = source.Select(ToRow).ToList(),
        };
    }

    private static DisputeRowVm ToRow(DisputeResponse d)
    {
        var isOpen = string.Equals(d.Status, "Open", StringComparison.OrdinalIgnoreCase);
        var isUnderReview = string.Equals(d.Status, "UnderReview", StringComparison.OrdinalIgnoreCase);
        var isEscalated = string.Equals(d.Status, "Escalated", StringComparison.OrdinalIgnoreCase);

        return new DisputeRowVm
        {
            Id = d.Id,
            PaymentId = d.PaymentId,
            UserId = d.UserId,
            Reason = d.Reason,
            Description = d.Description,
            Status = d.Status,
            Resolution = d.Resolution,
            ResolutionNotes = d.ResolutionNotes,
            ResolvedAt = d.ResolvedAt,
            CreatedAt = d.CreatedAt,
            CanReview = isOpen,
            CanResolve = isUnderReview || isEscalated,
            CanEscalate = isOpen || isUnderReview,
        };
    }

    public static DisputeStatusCountsVm ToStatusCountsVm(DisputeStatusCountsResponse r) => new()
    {
        Open        = r.Open,
        UnderReview = r.UnderReview,
        Resolved    = r.Resolved,
        Escalated   = r.Escalated,
        Closed      = r.Closed,
    };

    public static string StatusColor(string status) => status switch
    {
        "Resolved" => "success",
        "UnderReview" => "info",
        "Open" => "warning",
        "Escalated" => "danger",
        "Closed" => "secondary",
        _ => "secondary",
    };

    public static string StatusIcon(string status) => status switch
    {
        "Resolved" => "circle-check",
        "UnderReview" => "magnifying-glass",
        "Open" => "circle-exclamation",
        "Escalated" => "triangle-exclamation",
        "Closed" => "circle-xmark",
        _ => "circle-question",
    };
}
