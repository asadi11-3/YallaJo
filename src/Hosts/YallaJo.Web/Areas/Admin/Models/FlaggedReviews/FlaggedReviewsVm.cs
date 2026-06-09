namespace YallaJo.Web.Areas.Admin.Models.FlaggedReviews;

public sealed class FlaggedReviewsVm
{
    public IReadOnlyList<FlaggedReviewRowVm> Reviews { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class FlaggedReviewRowVm
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? ReviewerEmail { get; set; }
    public string TargetType { get; set; } = "";
    public Guid TargetId { get; set; }
    public decimal Rating { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsVerifiedBooking { get; set; }
    public bool ProfanityFlagged { get; set; }
    public int CurrentReportCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RowVersion { get; set; } = "";
}
