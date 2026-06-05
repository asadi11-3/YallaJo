namespace YallaJo.Web.Areas.Guide.Models.Reviews;

/// <summary>View model for the guide's Reviews page (read-only).</summary>
public sealed class ReviewsVm
{
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public IReadOnlyList<ReviewRowVm> Reviews { get; init; } = [];

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }

    public bool HasReviews => Reviews.Count > 0;
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public sealed record ReviewRowVm(
    Guid Id,
    decimal Rating,
    string? Title,
    string Content,
    string Status,
    bool IsVerifiedBooking,
    int HelpfulVoteCount,
    DateOnly? VisitDate,
    DateTime CreatedAt,
    IReadOnlyList<ReviewReplyRowVm> Replies);

public sealed record ReviewReplyRowVm(
    Guid Id,
    string Content,
    DateTime CreatedAt);
