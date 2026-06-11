using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Payouts;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Payout.Read)]
public sealed class PayoutsController : BaseController
{
    private const int DefaultPageSize = 25;
    private readonly PayoutsFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PayoutsController(PayoutsFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] PayoutsFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Payouts";

        // UI-UX-D1/R4: cap the page size at 50 at the controller boundary.
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var result = await _facade.GetIndexAsync(request.Cursor, pageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        // S1/PE1 — same action serves the full page and the listing.js fragment
        // (WantsAjax = X-Requested-With: fetch). No [OutputCache] ever (C2).
        if (!result.IsSuccess || result.Data is null)
        {
            var fallback = new PayoutsVm();
            if (WantsAjax())
            {
                ViewBag.Error = result.Error;
                return PartialView("_PayoutsResults", fallback);
            }

            SetError(result.Error);
            return View(fallback);
        }

        return WantsAjax() ? PartialView("_PayoutsResults", result.Data) : View(result.Data);
    }

    [HttpPost("admin/payouts/trigger")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Payout.Trigger)]
    public async Task<IActionResult> Trigger(CancellationToken ct)
    {
        var result = await _facade.TriggerAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Payouts.Flash.BatchTriggered"].Value, _localizer["Admin.Payouts.Flash.BatchTriggerFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/payouts/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Payout.Approve)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await _facade.ApproveAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Payouts.Flash.Approved"].Value, _localizer["Admin.Payouts.Flash.ApproveFailed"].Value);
        return RedirectToAction(nameof(Index));
    }
}
