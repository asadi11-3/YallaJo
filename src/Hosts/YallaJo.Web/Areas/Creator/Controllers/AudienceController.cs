using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Creator.Facades;
using YallaJo.Web.Areas.Creator.Models.Audience;
using YallaJo.Web.Areas.Creator.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Creator.Controllers;

/// <summary>
/// Read-only Creator Audience / Followers page (CCD-7). Shows the creator the
/// follower information for their own profile. Gated by <c>Creator.Read</c>.
/// No POST actions; no follow/unfollow here.
/// <para>
/// States: no profile → redirect to Application; not Active (Suspended/Deactivated)
/// → audience-unavailable notice; Active → authoritative follower count + an
/// anonymous, paged follower list. The VM/mapper never expose raw follower GUIDs or
/// any private/internal identifier.
/// </para>
/// </summary>
[Area("Creator")]
[Authorize]
public sealed class AudienceController : BaseController
{
    private readonly CreatorAudienceFacade _facade;

    public AudienceController(CreatorAudienceFacade facade) => _facade = facade;

    // GET /creator/audience
    [HttpGet("creator/audience")]
    [RequirePermission(WebPermission.Creator.Read)]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await _facade.GetAudienceAsync(page, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            SetSidebar();
            return View(CreatorAudienceMapper.Unavailable());
        }

        // No creator profile yet → send the user to the Application/Onboarding flow.
        if (result.Data.State == AudienceState.NoProfile)
            return RedirectToAction("Index", "Application");

        SetSidebar();

        // S1/PE1: AJAX pagination returns the followers fragment; plain GET renders the full page.
        return WantsAjax() ? PartialView("_AudienceResults", result.Data) : View(result.Data);
    }

    private void SetSidebar()
    {
        ViewData["CreatorNav"] = "Audience";
        ViewBag.Sidebar = new CreatorSidebarVm { DisplayName = User.Identity?.Name ?? "Creator" };
    }
}
