using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AccessibilityReviewsController(AccessibilityReviewsFacade reviews, IStringLocalizer<SharedResource> localizer)
    {
        _reviews = reviews;
        _localizer = localizer;
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

        if (result.IsSuccess) SetSuccess(_localizer["Accounts.Msg.AccReviewDeleted"]);
        else SetError(result.Error ?? _localizer["Accounts.Msg.AccReviewDeleteFailed"].Value);

        return RedirectToAction("Index", "Reviews", new { area = "Accounts", tab = "accessibility" });
    }
}
