using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Recommendations;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Batch.Read)]
public sealed class RecommendationsController : BaseController
{
    private readonly RecommendationsFacade _facade;

    public RecommendationsController(RecommendationsFacade facade) => _facade = facade;

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
    public async Task<IActionResult> RefreshBatches(CancellationToken ct)
    {
        var result = await _facade.RefreshBatchesAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Recommendation batches refreshed.", "Could not refresh the recommendation batches.");
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

        SetFlash(result, "Boost package created.", "Could not create the boost package.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/recommendations/boosts/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BoostPackage.Delete)]
    public async Task<IActionResult> DeactivateBoost([FromForm] Guid boostId, CancellationToken ct)
    {
        if (boostId == Guid.Empty)
        {
            SetError("A boost package id is required.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.DeactivateBoostAsync(boostId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Boost package deactivated.", "Could not deactivate the boost package.");
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

        SetFlash(result, "Editorial pin created.", "Could not create the editorial pin.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/recommendations/pins/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EditorialPin.Delete)]
    public async Task<IActionResult> DeactivatePin([FromForm] Guid pinId, CancellationToken ct)
    {
        if (pinId == Guid.Empty)
        {
            SetError("An editorial pin id is required.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.DeactivatePinAsync(pinId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Editorial pin deactivated.", "Could not deactivate the editorial pin.");
        return RedirectToAction(nameof(Index));
    }
}
