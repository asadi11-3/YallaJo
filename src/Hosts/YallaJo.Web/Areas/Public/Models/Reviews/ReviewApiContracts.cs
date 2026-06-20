namespace YallaJo.Web.Areas.Public.Models.Reviews;

// Public reviews-list item (subset of the API ReviewDto; no author display name is exposed by the API).
public sealed class PublicReviewResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string TargetType { get; init; } = "";
    public Guid TargetId { get; init; }
    public decimal Rating { get; init; }
    public string? Title { get; init; }
    public string Content { get; init; } = "";
    public DateOnly? VisitDate { get; init; }
    public bool IsVerifiedBooking { get; init; }
    public DateTime? LastEditedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public int HelpfulVoteCount { get; init; }
    public string RowVersion { get; init; } = "";

    // Public review image URLs (relative /uploads/... paths) returned by the API for Published reviews.
    public IReadOnlyList<string> ImageUrls { get; init; } = [];
}

// Mirrors the API PublicReviewPageDto(Items, Page, PageSize, TotalCount).
public sealed class PublicReviewPageResponse
{
    public List<PublicReviewResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}

// Mirrors the API RatingSummaryDto(EntityType, EntityId, AverageRating, ReviewCount).
public sealed class RatingSummaryResponse
{
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
}

// Mirrors the API ReviewEligibilityDto(CanReview, AlreadyReviewed).
public sealed class ReviewEligibilityResponse
{
    public bool CanReview { get; init; }
    public bool AlreadyReviewed { get; init; }
}

// Request bodies sent to the API. Field names MUST match the API binding records.
// CreateReviewRequest(ReviewTargetType TargetType, Guid TargetId, decimal Rating, string? Title, string Content, DateOnly? VisitDate)
public sealed record CreateReviewBody(string TargetType, Guid TargetId, decimal Rating, string? Title, string Content, DateOnly? VisitDate);

// EditReviewRequest(decimal Rating, string? Title, string Content, DateOnly? VisitDate, string? RowVersion)
public sealed record EditReviewBody(decimal Rating, string? Title, string Content, DateOnly? VisitDate, string? RowVersion);

// DeleteReviewRequest(string? RowVersion)
public sealed record DeleteReviewBody(string? RowVersion);

// ReviewReportRequest(ReportReason Reason, string Description)
public sealed record ReviewReportBody(string Reason, string Description);

// SubmitReportRequest(ReportableEntityType EntityType, Guid EntityId, ReportReason Reason, string Description)
public sealed record SubmitReportBody(string EntityType, Guid EntityId, string Reason, string Description);

// Response from POST /api/v1/content-core/attachments when uploading a review image.
public sealed class UploadReviewImageResponse
{
    public Guid Id { get; init; }
    public string Url { get; init; } = "";
}
