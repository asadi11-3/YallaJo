using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Analytics;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class AnalyticsController : GuideBaseController
{
    private readonly GuideAnalyticsFacade _analytics;

    public AnalyticsController(GuideAnalyticsFacade analytics) => _analytics = analytics;

    [HttpGet("guide/analytics")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetNav("Analytics");

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
}
