using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
[RequirePermission(WebPermission.GuideDashboard.Read)]
public sealed class TierController : BaseController
{
    private readonly GuideTierFacade _tier;

    public TierController(GuideTierFacade tier) => _tier = tier;

    [HttpGet("guide/tier")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["GuideNav"] = "Tier";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };

        var result = await _tier.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new TierProgressVm());
        }

        return View(result.Data);
    }
}
