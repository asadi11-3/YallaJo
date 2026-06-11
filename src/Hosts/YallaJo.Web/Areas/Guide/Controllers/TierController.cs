using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Guide.Controllers;

[RequirePermission(WebPermission.GuideDashboard.Read)]
public sealed class TierController : GuideBaseController
{
    private readonly GuideTierFacade _tier;

    public TierController(GuideTierFacade tier) => _tier = tier;

    [HttpGet("guide/tier")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetNav("Tier");

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
