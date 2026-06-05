using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Accounts.Models.Reviews;

public sealed class ReviewsVm
{
    public IReadOnlyList<ReviewRowVm> Reviews { get; set; } = [];
    public EditReviewFormVm EditForm { get; set; } = new();
    public bool HasReviews => Reviews.Count > 0;
}

public sealed record ReviewRowVm(
    Guid Id,
    string TargetType,
    Guid TargetId,
    string TargetLabel,
    string? TargetUrl,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string Status,
    bool IsVerifiedBooking,
    int HelpfulVoteCount,
    DateTime? LastEditedAt,
    DateTime CreatedAt,
    string RowVersion,
    IReadOnlyList<ReviewReplyRowVm> Replies)
{
    // Reviews are editable for 48h after creation (backend S-R3 window).
    public bool CanEdit => Status is "Published" or "AwaitingModeration"
        && DateTime.UtcNow - CreatedAt <= TimeSpan.FromHours(48);

    public bool IsPending => Status is "AwaitingModeration" or "AutoHidden";
}

public sealed record ReviewReplyRowVm(
    Guid Id,
    string Content,
    DateTime CreatedAt,
    DateTime? LastEditedAt);

public sealed class EditReviewFormVm
{
    [Required]
    public Guid Id { get; set; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    [Display(Name = "Rating")]
    public decimal Rating { get; set; } = 5;

    [StringLength(150)]
    [Display(Name = "Title")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Please write your review.")]
    [StringLength(4000, MinimumLength = 1)]
    [Display(Name = "Your review")]
    public string Content { get; set; } = "";

    [DataType(DataType.Date)]
    [Display(Name = "Visit date")]
    public DateOnly? VisitDate { get; set; }

    public string? RowVersion { get; set; }
}
