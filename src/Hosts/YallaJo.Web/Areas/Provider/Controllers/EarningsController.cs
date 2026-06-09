// ─────────────────────────────────────────────────────────────────────────────
// DEAD CONTROLLER — commented out by dead-controller audit (2026-06-09).
//
// Reachability audit found ZERO inbound references anywhere in the Web project:
//   • Not in _ProviderSidebar.cshtml (no nav entry).
//   • No view links / forms / Url.Action(...) / RedirectToAction(...) target it.
//   • Route /provider/earnings is unreachable from any UI.
//
// Earnings data is shown on the unified /provider/finance page (FinanceController),
// which calls EarningsFacade directly. This standalone Earnings page was orphaned
// (the GET-only Index has no write actions for the finance tabs to post to).
//
// Sibling orphan: Areas/Provider/Views/Earnings/Index.cshtml is now functionally
// dead too and can be deleted in a follow-up cleanup.
//
// Distinct from Areas/Guide/Controllers/EarningsController.cs, which is USED
// (linked from _GuideSidebar.cshtml and the Guide Earnings/Index pager).
// ─────────────────────────────────────────────────────────────────────────────
/*
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Earnings;
using YallaJo.Web.Areas.Provider.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class EarningsController : BaseController
{
    private readonly EarningsFacade _earnings;

    public EarningsController(EarningsFacade earnings) => _earnings = earnings;

    [HttpGet("provider/earnings")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["ProviderNav"] = "Earnings";
        ViewBag.Sidebar = new ProviderSidebarVm { DisplayName = User.Identity?.Name ?? "Provider" };

        var result = await _earnings.GetEarningsAsync(ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new EarningsVm());
        }

        return View(result.Data);
    }
}
*/
