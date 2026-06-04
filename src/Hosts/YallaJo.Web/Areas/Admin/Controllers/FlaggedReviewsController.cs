using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.FlaggedReviews;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminModerationQueue.Read)]
public sealed class FlaggedReviewsController : BaseController
{
    private readonly FlaggedReviewsFacade _facade;

    public FlaggedReviewsController(FlaggedReviewsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FlaggedReviewsFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "FlaggedReviews";
        var result = await _facade.GetIndexAsync(request.AfterCursor, request.PageSize, ct);
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

        SetFlash(result, "Review approved.", "Could not approve the review.");
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

        SetFlash(result, "Review removed.", "Could not remove the review.");
        return RedirectToAction(nameof(Index));
    }
}
