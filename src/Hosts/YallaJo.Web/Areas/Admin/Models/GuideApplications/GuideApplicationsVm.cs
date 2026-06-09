namespace YallaJo.Web.Areas.Admin.Models.GuideApplications;

public sealed class GuideApplicationsVm
{
    public Guid? TourId { get; set; }
    public string? StatusFilter { get; set; }
    public IReadOnlyList<GuideApplicationRowVm> Applications { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }

    // Tour-name options for the F10-compliant tour picker (id travels as option value).
    public IReadOnlyList<GuideTourOptionVm> TourOptions { get; set; } = [];
}

public sealed class GuideTourOptionVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class GuideApplicationRowVm
{
    public Guid ApplicationId { get; set; }
    public Guid TourId { get; set; }
    public Guid TourGuideId { get; set; }
    public Guid GuideUserId { get; set; }
    public string? TourTitle { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? RelevantExperience { get; set; }
    public decimal? ProposedBasePrice { get; set; }
    public int ResubmissionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Resolved human identity of the applying guide (F10). Null when not resolvable.
    public string? GuideEmail { get; set; }
}
