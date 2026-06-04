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
