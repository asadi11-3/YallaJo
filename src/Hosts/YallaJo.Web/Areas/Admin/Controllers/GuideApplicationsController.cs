using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.GuideApplications;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.GuideApplication.Read)]
public sealed class GuideApplicationsController : BaseController
{
    private readonly GuideApplicationsFacade _facade;

    public GuideApplicationsController(GuideApplicationsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid? tourId = null,
        string? status = null,
        int page = 1,
        CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        ViewData["AdminNav"] = "GuideApplications";

        var result = await _facade.GetIndexAsync(tourId, status, page, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        // S1/PE1 — same action serves the full page and the listing.js fragment
        // (WantsAjax = X-Requested-With: fetch). No [OutputCache] ever (C2).
        if (!result.IsSuccess || result.Data is null)
        {
            var fallback = new GuideApplicationsVm();
            if (WantsAjax())
            {
                ViewBag.Error = result.Error;
                return PartialView("_GuideApplicationsResults", fallback);
            }

            SetError(result.Error);
            return View(fallback);
        }

        return WantsAjax() ? PartialView("_GuideApplicationsResults", result.Data) : View(result.Data);
    }

    [HttpPost("admin/guide-applications/{tourId:guid}/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.GuideApplication.Approve)]
    public async Task<IActionResult> Approve(Guid tourId, Guid id, CancellationToken ct)
    {
        var result = await _facade.ApproveAsync(tourId, id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Guide application approved.", "Could not approve the guide application.");
        return Back(tourId);
    }

    [HttpPost("admin/guide-applications/{tourId:guid}/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.GuideApplication.Reject)]
    public async Task<IActionResult> Reject(Guid tourId, Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A rejection reason is required.");
            return Back(tourId);
        }

        var result = await _facade.RejectAsync(tourId, id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Guide application rejected.", "Could not reject the guide application.");
        return Back(tourId);
    }

    private IActionResult Back(Guid? tourId) => RedirectToAction(nameof(Index), new { tourId });
}
