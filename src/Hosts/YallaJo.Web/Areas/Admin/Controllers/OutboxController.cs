using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Outbox;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Outbox.Read)]
public sealed class OutboxController : BaseController
{
    private readonly OutboxFacade _facade;

    public OutboxController(OutboxFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] OutboxFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Outbox";
        var result = await _facade.GetIndexAsync(request, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        // S1/PE1 — same action serves the full page and the listing.js fragment
        // (WantsAjax = X-Requested-With: fetch). No [OutputCache] ever (C2).
        if (!result.IsSuccess || result.Data is null)
        {
            var fallback = new OutboxVm();
            if (WantsAjax())
            {
                ViewBag.Error = result.Error;
                return PartialView("_OutboxResults", fallback);
            }

            SetError(result.Error);
            return View(fallback);
        }

        return WantsAjax() ? PartialView("_OutboxResults", result.Data) : View(result.Data);
    }

    [HttpPost("admin/outbox/{module}/{id:guid}/replay")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Outbox.Replay)]
    public async Task<IActionResult> Replay(string module, Guid id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            SetError("A module is required.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.ReplayAsync(module, id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Outbox message replayed.", "Could not replay the outbox message.");
        return RedirectToAction(nameof(Index));
    }

    // ── §8.12: re-emit TourApproved events to repopulate Booking snapshots ──────────
    [HttpPost("admin/outbox/backfill/tour-snapshots")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Outbox.Replay)]
    public async Task<IActionResult> BackfillTourSnapshots(int? batchSize, CancellationToken ct)
    {
        var result = await _facade.BackfillTourSnapshotsAsync(batchSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Tour-snapshot backfill started.", "Could not start the tour-snapshot backfill.");
        return RedirectToAction(nameof(Index));
    }
}
