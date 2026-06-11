using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Recommendations;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Batch.Read)]
public sealed class RecommendationsController : BaseController
{
    private readonly RecommendationsFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RecommendationsController(RecommendationsFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AdminNav"] = "Recommendations";
        var result = await _facade.GetIndexAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new RecommendationsVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/recommendations/batches/refresh")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Batch.Refresh)]
    public async Task<IActionResult> RefreshBatches([FromForm] RefreshBatchRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.SourceKind) || req.SourceId == Guid.Empty || string.IsNullOrWhiteSpace(req.Context))
        {
            SetError(_localizer["Admin.Recommendations.Flash.RefreshFieldsRequired"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.RefreshBatchesAsync(req, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Recommendations.Flash.BatchRefreshed"].Value, _localizer["Admin.Recommendations.Flash.BatchRefreshFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/recommendations/boosts")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BoostPackage.Create)]
    public async Task<IActionResult> CreateBoost([FromForm] CreateBoostRequest req, CancellationToken ct)
    {
        var result = await _facade.CreateBoostAsync(req, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Recommendations.Flash.BoostCreated"].Value, _localizer["Admin.Recommendations.Flash.BoostCreateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/recommendations/boosts/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BoostPackage.Delete)]
    public async Task<IActionResult> DeactivateBoost([FromForm] Guid boostId, CancellationToken ct)
    {
        if (boostId == Guid.Empty)
        {
            SetError(_localizer["Admin.Recommendations.Flash.BoostIdRequired"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.DeactivateBoostAsync(boostId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Recommendations.Flash.BoostDeactivated"].Value, _localizer["Admin.Recommendations.Flash.BoostDeactivateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/recommendations/pins")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EditorialPin.Create)]
    public async Task<IActionResult> CreatePin([FromForm] CreatePinRequest req, CancellationToken ct)
    {
        var result = await _facade.CreatePinAsync(req, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Recommendations.Flash.PinCreated"].Value, _localizer["Admin.Recommendations.Flash.PinCreateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/recommendations/pins/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EditorialPin.Delete)]
    public async Task<IActionResult> DeactivatePin([FromForm] Guid pinId, CancellationToken ct)
    {
        if (pinId == Guid.Empty)
        {
            SetError(_localizer["Admin.Recommendations.Flash.PinIdRequired"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.DeactivatePinAsync(pinId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Recommendations.Flash.PinDeactivated"].Value, _localizer["Admin.Recommendations.Flash.PinDeactivateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }
}
