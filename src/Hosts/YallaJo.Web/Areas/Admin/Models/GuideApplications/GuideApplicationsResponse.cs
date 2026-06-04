namespace YallaJo.Web.Areas.Admin.Models.GuideApplications;

/// <summary>
/// Mirrors the ContentTours <c>ListGuideApplicationsResult</c> returned by
/// GET /api/v1/tours/{tourId}/applications. The backend returns a flat
/// { Items, TotalCount } shape (not a PaginatedResult), so paging metadata
/// is computed web-side from the requested page/pageSize against TotalCount.
/// </summary>
public sealed class GuideApplicationPageResponse
{
    public IReadOnlyList<GuideApplicationItemResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
}

public sealed class GuideApplicationItemResponse
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
    public Guid? ReviewedByAdminId { get; set; }
    public string? RejectionReason { get; set; }
}
