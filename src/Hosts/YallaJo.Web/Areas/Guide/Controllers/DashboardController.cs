using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Guide.Controllers;

[RequirePermission(WebPermission.GuideDashboard.Read)]
public sealed class DashboardController : GuideBaseController
{
    private readonly GuideDashboardFacade _dashboard;

    public DashboardController(GuideDashboardFacade dashboard) => _dashboard = dashboard;

    [HttpGet("guide")]
    [HttpGet("guide/dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetNav("Dashboard");

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
