using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Home;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminDashboard.Read)]
public sealed class HomeController : BaseController
{
    private readonly HomeFacade _home;

    public HomeController(HomeFacade home) => _home = home;

    [HttpGet("admin")]
    [HttpGet("admin/dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AdminNav"] = "Dashboard";

        var result = await _home.GetDashboardAsync(ct);
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
