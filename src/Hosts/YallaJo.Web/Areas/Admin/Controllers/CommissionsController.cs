using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Commissions;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.CommissionRule.Read)]
public sealed class CommissionsController(CommissionsFacade facade) : BaseController
{
    private readonly CommissionsFacade _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] CommissionsFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Commissions";
        var result = await _facade.GetIndexAsync(request.Tier, request.Currency, request.IncludeInactive, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new CommissionsVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/commissions")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.CommissionRule.Create)]
    public async Task<IActionResult> Create([FromForm] CreateCommissionRuleRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Tier) || string.IsNullOrWhiteSpace(request.Currency) || request.Percentage <= 0)
        {
            SetError("Tier, currency, and a positive percentage are required.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.CreateAsync(request, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Commission rule created.", "Could not create the commission rule.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/commissions/{id:guid}/update")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.CommissionRule.Update)]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateCommissionRuleRequest request, CancellationToken ct)
    {
        if (request.Percentage <= 0)
        {
            SetError("A positive percentage is required.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.UpdateAsync(id, request, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Commission rule updated.", "Could not update the commission rule.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/commissions/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.CommissionRule.Delete)]
    public async Task<IActionResult> Delete(Guid id, string? reason, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, reason, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Commission rule deleted.", "Could not delete the commission rule.");
        return RedirectToAction(nameof(Index));
    }
}
