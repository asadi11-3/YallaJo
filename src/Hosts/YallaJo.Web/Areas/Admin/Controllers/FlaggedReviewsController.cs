using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.FlaggedReviews;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminModerationQueue.Read)]
public sealed class FlaggedReviewsController : BaseController
{
    private readonly FlaggedReviewsFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public FlaggedReviewsController(FlaggedReviewsFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FlaggedReviewsFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "FlaggedReviews";
        // UI-UX-D1/R4: cap the page size at 50 at the controller boundary.
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var result = await _facade.GetIndexAsync(request.AfterCursor, pageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new FlaggedReviewsVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/flagged-reviews/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminModerationQueue.Approve)]
    public async Task<IActionResult> Approve(Guid id, string? rowVersion, string? notes, CancellationToken ct)
    {
        var result = await _facade.ApproveAsync(id, rowVersion, notes, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (WantsAjax())
        {
            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error ?? _localizer["Admin.FlaggedReviews.Flash.ApproveFailed"].Value });
            }

            return await ListPartialAsync(ct);
        }

        SetFlash(result, _localizer["Admin.FlaggedReviews.Flash.Approved"].Value, _localizer["Admin.FlaggedReviews.Flash.ApproveFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/flagged-reviews/{id:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminModerationQueue.Remove)]
    public async Task<IActionResult> Remove(Guid id, string? rowVersion, string? notes, CancellationToken ct)
    {
        var result = await _facade.RemoveAsync(id, rowVersion, notes, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (WantsAjax())
        {
            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error ?? _localizer["Admin.FlaggedReviews.Flash.RemoveFailed"].Value });
            }

            return await ListPartialAsync(ct);
        }

        SetFlash(result, _localizer["Admin.FlaggedReviews.Flash.Removed"].Value, _localizer["Admin.FlaggedReviews.Flash.RemoveFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ──

    /// <summary>
    /// Re-fetches the flagged-reviews queue and returns the swappable list partial for
    /// AJAX approve/remove. The default page size mirrors the Index controller boundary.
    /// </summary>
    private async Task<IActionResult> ListPartialAsync(CancellationToken ct)
    {
        var result = await _facade.GetIndexAsync(null, 50, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        return PartialView("_FlaggedReviewsList", result.Data ?? new FlaggedReviewsVm());
    }
}
