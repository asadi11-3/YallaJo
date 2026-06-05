using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Earnings;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class EarningsController : BaseController
{
    private const int DefaultPageSize = 20;

    private readonly GuideEarningsFacade _earnings;

    public EarningsController(GuideEarningsFacade earnings) => _earnings = earnings;

    [HttpGet("guide/earnings")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        SetSidebar();

        var result = await _earnings.GetAsync(page, DefaultPageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new EarningsVm());
        }

        return View(result.Data);
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "Earnings";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
