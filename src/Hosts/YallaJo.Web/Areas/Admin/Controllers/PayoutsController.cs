using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Payouts;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Payout.Read)]
public sealed class PayoutsController : BaseController
{
    private const int DefaultPageSize = 25;
    private readonly PayoutsFacade _facade;

    public PayoutsController(PayoutsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] PayoutsFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Payouts";

        var result = await _facade.GetIndexAsync(request.Cursor, request.PageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new PayoutsVm());
        }

        return View(result.Data);
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

        SetFlash(result, "Payout batch triggered.", "Could not trigger the payout batch.");
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

        SetFlash(result, "Payout approved.", "Could not approve the payout.");
        return RedirectToAction(nameof(Index));
    }
}
