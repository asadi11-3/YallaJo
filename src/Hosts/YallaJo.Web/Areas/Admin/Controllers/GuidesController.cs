using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Guides;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.TourGuideProfile.Read)]
public sealed class GuidesController : BaseController
{
    private readonly GuidesFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public GuidesController(GuidesFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

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
            SetError(_localizer["Admin.Shared.Flash.SuspendReasonRequired"].Value);
            return Back(id);
        }

        var result = await _facade.SuspendAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Guides.Flash.Suspended"].Value, _localizer["Admin.Guides.Flash.SuspendFailed"].Value);
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

        SetFlash(result, _localizer["Admin.Guides.Flash.Reinstated"].Value, _localizer["Admin.Guides.Flash.ReinstateFailed"].Value);
        return Back(id);
    }

    // ── POST /admin/guides/{id}/edit ────────────────────────────────────────────────
    // §8.14 — admin edits a guide profile (PUT /guides/admin/{guideId}).
    [HttpPost("admin/guides/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TourGuideProfile.Update)]
    public async Task<IActionResult> Edit(Guid id, AdminEditGuideVm form, CancellationToken ct)
    {
        form.Id = id;

        if (!ModelState.IsValid)
        {
            SetError(_localizer["Admin.Guides.Flash.FixFields"].Value);
            return Back(id);
        }

        var result = await _facade.UpdateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Guides.Flash.ProfileUpdated"].Value, _localizer["Admin.Guides.Flash.ProfileUpdateFailed"].Value);
        return Back(id);
    }

    // ── POST /admin/guides/{id}/delete ──────────────────────────────────────────────
    // §8.14 — admin deactivates a guide (DELETE /guides/admin/{guideId}).
    [HttpPost("admin/guides/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TourGuideProfile.DeleteAny)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            SetSuccess(_localizer["Admin.Guides.Flash.Deactivated"].Value);
            return RedirectToAction(nameof(Index));
        }

        SetError(result.Error ?? _localizer["Admin.Guides.Flash.DeactivateFailed"].Value);
        return Back(id);
    }

    private IActionResult Back(Guid id) => RedirectToAction(nameof(Index), new { id });
}
