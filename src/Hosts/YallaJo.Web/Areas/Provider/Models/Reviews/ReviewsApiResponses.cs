namespace YallaJo.Web.Areas.Provider.Models.Reviews;

// GET /api/v1/tours/provider/my-tours (slim projection for the tour selector)
public sealed class MyToursResponse
{
    public IReadOnlyList<TourOptionResponse> Items { get; init; } = [];
}

public sealed class TourOptionResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

// GET /api/v1/social/reviews/{entityType}/{entityId}
public sealed class PublicReviewPageResponse
{
    public IReadOnlyList<ReviewResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}

public sealed class ReviewResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public decimal Rating { get; init; }
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? Status { get; init; }
    public bool IsVerifiedBooking { get; init; }
    public int HelpfulVoteCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<ReviewReplyResponse> Replies { get; init; } = [];
}

public sealed class ReviewReplyResponse
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

// GET /api/v1/social/reviews/ratings
public sealed class RatingSummaryResponse
{
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
}

// POST /api/v1/social/reviews/{id}/reply
public sealed record AddReplyRequest(string Content);

// POST /api/v1/social/reviews/{id}/report (Reason = ReportReason enum name)
public sealed record ReviewReportRequest(string Reason, string Description);

/// <summary>[Backend] B6 mirror of RatingSummaryBatchItemDto.</summary>
public sealed record RatingSummaryBatchItemResponse(Guid EntityId, decimal Average, int Count);
