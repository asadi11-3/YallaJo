namespace YallaJo.Web.Areas.Provider.Models.TourApplications;

// GET /api/v1/tours/{tourId}/applications  → { items[], totalCount }
public sealed class ListGuideApplicationsResponse
{
    public IReadOnlyList<GuideApplicationResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
}

public sealed class GuideApplicationResponse
{
    public Guid ApplicationId { get; init; }
    public Guid TourId { get; init; }
    public string? TourTitle { get; init; }
    public Guid TourGuideId { get; init; }
    public Guid GuideUserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string? RelevantExperience { get; init; }
    public decimal? ProposedBasePrice { get; init; }
    public int ResubmissionCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
}

// POST /api/v1/tours/{tourId}/applications/{applicationId}/reject
public sealed record RejectGuideApplicationApiRequest(string Reason);
