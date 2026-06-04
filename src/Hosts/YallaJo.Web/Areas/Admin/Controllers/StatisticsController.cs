using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Statistics;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Interaction.Read)]
public sealed class StatisticsController : BaseController
{
    private readonly StatisticsFacade _statistics;

    public StatisticsController(StatisticsFacade statistics) => _statistics = statistics;

    [HttpGet("admin/analytics")]
    public async Task<IActionResult> Index(
        [FromQuery] StatisticsFilterRequest request,
        CancellationToken ct)
    {
        ViewData["AdminNav"] = "Statistics";

        var result = await _statistics.GetAnalyticsAsync(request, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new StatisticsVm());
        }

        return View(result.Data);
    }
}
