using YallaJo.Web.Areas.Public.Models.AccessibilityReviews;

namespace YallaJo.Web.Areas.Accounts.Models.Reviews;

/// <summary>
/// Phase 3 (Accounts plan): Reviews hub composing my reviews + my accessibility reviews
/// as tabs on /accounts/reviews. Accessibility list loads best-effort.
/// </summary>
public sealed class ReviewsHubVm
{
    public ReviewsVm Reviews { get; init; } = new();
    public MyAccessibilityReviewsVm Accessibility { get; init; } = new();
    public string ActiveTab { get; init; } = "mine";
}
