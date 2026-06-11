using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// "My Accessibility Reviews". Phase 3 (Accounts master plan): the standalone page moved
/// into the Reviews hub (/accounts/reviews?tab=accessibility). Index now permanently
/// redirects there; Delete keeps its route, anti-forgery and permission gate, and PRGs
/// back to the hub tab. Edits still happen on the entity detail page (48h window).
/// </summary>
[Area("Accounts")]
[Authorize]
[RequirePermission(WebPermission.AccessibilityReview.Read)]
public sealed class AccessibilityReviewsController : BaseController
{
    private readonly AccessibilityReviewsFacade _reviews;

    public AccessibilityReviewsController(AccessibilityReviewsFacade reviews)
    {
        _reviews = reviews;
    }

    [HttpGet("accounts/accessibility-reviews")]
    public IActionResult Index()
        => RedirectPermanent(Url.Action("Index", "Reviews", new { area = "Accounts", tab = "accessibility" })!);

    [HttpPost("accounts/accessibility-reviews/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AccessibilityReview.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _reviews.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess) SetSuccess("Your accessibility review has been deleted.");
        else SetError(result.Error ?? "Could not delete the accessibility review.");

        return RedirectToAction("Index", "Reviews", new { area = "Accounts", tab = "accessibility" });
    }
}
