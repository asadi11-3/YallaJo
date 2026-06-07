using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Public.Models.AccessibilityReviews;

/// <summary>List of accessibility reviews for one entity, rendered by
/// <c>_AccessibilityReviews.cshtml</c>. Fed via <c>ViewData["AccessibilityReviews"]</c>.</summary>
public sealed class AccessibilityReviewListVm
{
    public string TargetType { get; init; } = "Place";
    public Guid TargetId { get; init; }
    public IReadOnlyList<AccessibilityReviewRowVm> Items { get; init; } = [];
    public int TotalCount { get; init; }

    public bool HasResults => Items.Count > 0;
}

public sealed record AccessibilityReviewRowVm(
    Guid Id,
    Guid UserId,
    string AuthorHandle,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    IReadOnlyList<string> FeatureTypes,
    DateTime CreatedAt,
    DateTime? LastEditedAt)
{
    /// <summary>True only within 48 hours of creation (mirrors the backend edit window).</summary>
    public bool IsWithinEditWindow => CreatedAt >= DateTime.UtcNow.AddHours(-48);
}

/// <summary>The caller's own accessibility reviews ("My Accessibility Reviews").</summary>
public sealed class MyAccessibilityReviewsVm
{
    public IReadOnlyList<MyAccessibilityReviewRowVm> Items { get; init; } = [];
    public bool HasItems => Items.Count > 0;
}

public sealed record MyAccessibilityReviewRowVm(
    Guid Id,
    string TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    IReadOnlyList<string> FeatureTypes,
    DateTime CreatedAt,
    DateTime? LastEditedAt)
{
    public bool IsWithinEditWindow => CreatedAt >= DateTime.UtcNow.AddHours(-48);

    /// <summary>Public detail URL for the reviewed entity (slug-less id links where applicable).</summary>
    public string? EntityUrl => TargetType switch
    {
        "Business" => $"/businesses/{TargetId}",
        _ => null, // Tour/Place/TourGuide are slug-routed; the id is not a slug, so no direct link.
    };

    public string EntityLabel => TargetType switch
    {
        "Tour" => "Tour",
        "Place" => "Place",
        "Business" => "Business",
        "TourGuide" => "Tour guide",
        _ => TargetType,
    };
}

/// <summary>Create/edit form for an accessibility review. Validation mirrors the backend
/// validator (rating 0.5–5 in 0.5 steps, content 10–5000, title ≤200, ≥1 feature type).</summary>
public sealed class AccessibilityReviewFormVm
{
    // Bound by the controller from the route/page, not user-editable.
    public string TargetType { get; set; } = "Place";
    public Guid TargetId { get; set; }

    [Range(0.5, 5.0, ErrorMessage = "Rating must be between 0.5 and 5.")]
    public decimal Rating { get; set; } = 5m;

    [StringLength(200, ErrorMessage = "Title must not exceed 200 characters.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Please describe the accessibility experience.")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "Review must be between 10 and 5000 characters.")]
    public string Content { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateOnly? VisitDate { get; set; }

    /// <summary>Selected accessibility feature kinds (checkbox names). Joined to CSV server-side.</summary>
    [MinLength(1, ErrorMessage = "Select at least one accessibility feature.")]
    public List<string> FeatureTypes { get; set; } = [];
}
