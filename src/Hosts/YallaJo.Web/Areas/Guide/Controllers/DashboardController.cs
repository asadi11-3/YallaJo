using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class DashboardController : BaseController
{
    private readonly GuideDashboardFacade _dashboard;

    public DashboardController(GuideDashboardFacade dashboard) => _dashboard = dashboard;

    [HttpGet("guide")]
    [HttpGet("guide/dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["GuideNav"] = "Dashboard";
        ViewBag.Sidebar = new GuideSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Guide"
        };

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
