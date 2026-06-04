using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Guides;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.TourGuideProfile.Read)]
public sealed class GuidesController : BaseController
{
    private readonly GuidesFacade _facade;

    public GuidesController(GuidesFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(Guid? id = null, CancellationToken ct = default)
    {
        ViewData["AdminNav"] = "Guides";

        var result = await _facade.GetIndexAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new GuidesVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/guides/{id:guid}/suspend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TourGuideProfile.Suspend)]
    public async Task<IActionResult> Suspend(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A suspension reason is required.");
            return Back(id);
        }

        var result = await _facade.SuspendAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Tour guide suspended.", "Could not suspend the tour guide.");
        return Back(id);
    }

    [HttpPost("admin/guides/{id:guid}/reinstate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TourGuideProfile.Reinstate)]
    public async Task<IActionResult> Reinstate(Guid id, CancellationToken ct)
    {
        var result = await _facade.ReinstateAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Tour guide reinstated.", "Could not reinstate the tour guide.");
        return Back(id);
    }

    private IActionResult Back(Guid id) => RedirectToAction(nameof(Index), new { id });
}
