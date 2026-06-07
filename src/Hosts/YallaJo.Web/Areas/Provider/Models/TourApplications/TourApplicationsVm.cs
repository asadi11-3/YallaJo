namespace YallaJo.Web.Areas.Provider.Models.TourApplications;

public sealed class TourApplicationsIndexVm
{
    public Guid TourId { get; init; }
    public string TourName { get; init; } = string.Empty;
    public string TourStatusLabel { get; init; } = string.Empty;
    public int TotalCount { get; init; }

    public List<TourApplicationRowVm> Applications { get; init; } = [];

    public bool HasApplications => Applications.Count > 0;
}

public sealed class TourApplicationRowVm
{
    public Guid ApplicationId { get; init; }
    public Guid GuideUserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string? RelevantExperience { get; init; }
    public decimal? ProposedBasePrice { get; init; }
    public int ResubmissionCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RejectionReason { get; init; }

    public bool IsPending => string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Status, "Submitted", StringComparison.OrdinalIgnoreCase);
    public bool IsApproved => string.Equals(Status, "Approved", StringComparison.OrdinalIgnoreCase);
    public bool IsRejected => string.Equals(Status, "Rejected", StringComparison.OrdinalIgnoreCase);

    // A11Y5: color + icon + text. Returns (bootstrap bg class, bootstrap icon name).
    public (string Css, string Icon) StatusBadge =>
        IsApproved ? ("text-bg-success", "bi-check-circle")
        : IsRejected ? ("text-bg-danger", "bi-x-circle")
        : IsPending ? ("text-bg-warning", "bi-hourglass-split")
        : ("text-bg-secondary", "bi-dot");
}
