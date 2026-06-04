using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class MyToursController : BaseController
{
    private readonly GuideMyToursFacade _myTours;

    public MyToursController(GuideMyToursFacade myTours) => _myTours = myTours;

    [HttpGet("guide/tours")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        SetSidebar();

        var result = await _myTours.GetAsync(page, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new GuideMyToursVm());
        }

        return View(result.Data);
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "MyTours";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
