using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.AgencyRoster;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

/// <summary>
/// Agency-OWNER roster management at <c>/guide/agency/roster</c>. Separate from the
/// guide self-service <see cref="AgencyController"/> (/guide/agency). Gated by the
/// AgencyRoster permission; the backend additionally enforces agency-ownership.
/// </summary>
[Area("Guide")]
[Authorize]
[RequirePermission(WebPermission.AgencyRoster.Read)]
public sealed class AgencyRosterController : BaseController
{
    private readonly GuideAgencyRosterFacade _facade;

    public AgencyRosterController(GuideAgencyRosterFacade facade) => _facade = facade;

    [HttpGet("guide/agency/roster")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();
        var result = await _facade.GetRosterAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new AgencyRosterVm());
        }

        return View(result.Data);
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "AgencyRoster";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
