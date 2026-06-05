using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Analytics;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class AnalyticsController : BaseController
{
    private readonly GuideAnalyticsFacade _analytics;

    public AnalyticsController(GuideAnalyticsFacade analytics) => _analytics = analytics;

    [HttpGet("guide/analytics")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();

        var result = await _analytics.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new AnalyticsVm());
        }

        return View(result.Data);
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "Analytics";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
