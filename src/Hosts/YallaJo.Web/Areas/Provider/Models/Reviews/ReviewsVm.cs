namespace YallaJo.Web.Areas.Provider.Models.Reviews;

public sealed class ReviewsVm
{
    public IReadOnlyList<TourOptionVm> Tours { get; init; } = [];
    public Guid? SelectedTourId { get; init; }
    public string? SelectedTourName { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public IReadOnlyList<ReviewRowVm> Reviews { get; init; } = [];

    public bool HasTours => Tours.Count > 0;
    public bool HasReviews => Reviews.Count > 0;

    // Report reasons surfaced in the report dropdown (match API ReportReason enum names).
    public static IReadOnlyList<string> ReportReasons { get; } =
        ["Spam", "Inappropriate", "Misleading", "Harassment", "FakeReview", "Other"];
}

public sealed class TourOptionVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class ReviewRowVm
{
    public Guid Id { get; init; }
    public decimal Rating { get; init; }
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsVerifiedBooking { get; init; }
    public int HelpfulVoteCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ReplyContent { get; init; }
    public Guid? ReplyId { get; init; }

    public bool HasReply => !string.IsNullOrWhiteSpace(ReplyContent) && ReplyId.HasValue;
}
