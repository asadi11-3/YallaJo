using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Public.Models.Reviews;

/// <summary>
/// Form posted when an authenticated user writes a review for a Place/Business/Tour.
/// TargetType is set server-side by the controller; the form carries the hidden TargetId.
/// Validation mirrors the API CreateReviewCommandValidator (Rating 1-5, Title &lt;= 150, Content 20-2000).
/// </summary>
public sealed class ReviewFormVm
{
    public string TargetType { get; set; } = "";

    public Guid TargetId { get; set; }

    [Range(1, 5, ErrorMessage = "Please choose a rating between 1 and 5.")]
    public decimal Rating { get; set; } = 5;

    [StringLength(150, ErrorMessage = "Title must be 150 characters or fewer.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Please write your review.")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Review must be between 20 and 2000 characters.")]
    public string Content { get; set; } = "";

    [DataType(DataType.Date)]
    public DateOnly? VisitDate { get; set; }
}

public sealed class ReviewEditFormVm
{
    public Guid ReviewId { get; set; }

    public string? RowVersion { get; set; }

    [Range(1, 5, ErrorMessage = "Please choose a rating between 1 and 5.")]
    public decimal Rating { get; set; } = 5;

    [StringLength(150, ErrorMessage = "Title must be 150 characters or fewer.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Please write your review.")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Review must be between 20 and 2000 characters.")]
    public string Content { get; set; } = "";

    [DataType(DataType.Date)]
    public DateOnly? VisitDate { get; set; }
}

/// <summary>Form posted when an authenticated user reports a Place/Business/Tour.</summary>
public sealed class ReportFormVm
{
    public string EntityType { get; set; } = "";

    public Guid EntityId { get; set; }

    [Required(ErrorMessage = "Please choose a reason.")]
    public string Reason { get; set; } = "";

    [Required(ErrorMessage = "Please add a few details.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Details must be between 5 and 1000 characters.")]
    public string Description { get; set; } = "";
}

/// <summary>Read-model for rendering the reviews list + rating summary on a detail page.</summary>
public sealed class ReviewListVm
{
    public string TargetType { get; init; } = "";

    public Guid TargetId { get; init; }

    public IReadOnlyList<ReviewItemVm> Items { get; init; } = [];

    public decimal AverageRating { get; init; }

    public int ReviewCount { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public bool HasResults => Items.Count > 0;

    /// <summary>
    /// True when the current authenticated user is eligible to submit a review
    /// (has a recent completed booking). Always false for anonymous requests.
    /// Drives the create-review form gating on the tour detail page.
    /// </summary>
    public bool CanReview { get; init; }

    /// <summary>True when the current authenticated user has already reviewed this target.</summary>
    public bool AlreadyReviewed { get; init; }
}

public sealed class ReviewItemVm
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public decimal Rating { get; init; }

    public string? Title { get; init; }

    public string Content { get; init; } = "";

    public DateOnly? VisitDate { get; init; }

    public bool IsVerifiedBooking { get; init; }

    public DateTime? LastEditedAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public int HelpfulVoteCount { get; init; }

    public string RowVersion { get; init; } = "";

    /// <summary>
    /// Absolute (asset-resolved) public image URLs attached to this review.
    /// Empty when the review has no images.
    /// </summary>
    public IReadOnlyList<string> ImageUrls { get; init; } = [];

    public bool IsWithinEditWindow => CreatedAt >= DateTime.UtcNow.AddHours(-48);
}

public static class ReviewMapper
{
    public static ReviewItemVm ToItem(PublicReviewResponse r, YallaJo.Web.Services.IApiAssetUrlResolver assetResolver) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        Rating = r.Rating,
        Title = r.Title,
        Content = r.Content,
        VisitDate = r.VisitDate,
        IsVerifiedBooking = r.IsVerifiedBooking,
        LastEditedAt = r.LastEditedAt,
        CreatedAt = r.CreatedAt,
        HelpfulVoteCount = r.HelpfulVoteCount,
        RowVersion = r.RowVersion,
        ImageUrls = r.ImageUrls
            .Select(assetResolver.Resolve)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u!)
            .ToList(),
    };
}
