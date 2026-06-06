using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Creator.Facades;
using YallaJo.Web.Areas.Creator.Models.Dashboard;
using YallaJo.Web.Areas.Creator.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Creator.Controllers;

[Area("Creator")]
[Authorize]
public sealed class DashboardController : BaseController
{
    private readonly CreatorDashboardFacade _dashboard;

    public DashboardController(CreatorDashboardFacade dashboard) => _dashboard = dashboard;

    [HttpGet("creator")]
    [HttpGet("creator/dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["CreatorNav"] = "Dashboard";
        ViewBag.Sidebar = new CreatorSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Creator"
        };

        var result = await _dashboard.GetDashboardAsync(ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new CreatorDashboardVm());
        }

        return View(result.Data);
    }
}
