using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Dashboard;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
[RequirePermission(WebPermission.ProviderDashboard.Read)]
public sealed class DashboardController : BaseController
{
    private readonly DashboardFacade _dashboard;

    public DashboardController(DashboardFacade dashboard) => _dashboard = dashboard;

    [HttpGet("provider")]
    [HttpGet("provider/dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var result = await _dashboard.GetDashboardAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new DashboardVm());
        }

        return View(result.Data);
    }
}
