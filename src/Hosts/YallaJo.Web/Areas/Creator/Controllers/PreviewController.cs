using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Creator.Facades;
using YallaJo.Web.Areas.Creator.Models.Preview;
using YallaJo.Web.Areas.Creator.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Creator.Controllers;

/// <summary>
/// Read-only public-profile preview (CCD-6): shows the creator how visitors see their
/// public profile, using the anonymous public-by-slug endpoints. Gated by
/// <c>Creator.Read</c>. No POST actions.
/// <para>
/// States: no profile → redirect to Application; not Active / public 404 →
/// preview-unavailable notice; Active → public-safe read-only preview. The VM/mapper
/// drops internal fields (UserId, Status, LinkedProviderId, CreatedAt).
/// </para>
/// </summary>
[Area("Creator")]
[Authorize]
public sealed class PreviewController : BaseController
{
    private readonly CreatorPreviewFacade _facade;

    public PreviewController(CreatorPreviewFacade facade) => _facade = facade;

    // GET /creator/preview
    [HttpGet("creator/preview")]
    [RequirePermission(WebPermission.Creator.Read)]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await _facade.GetPreviewAsync(page, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            SetSidebar();
            return View(PublicProfilePreviewMapper.Unavailable());
        }

        // No creator profile yet → send the user to the Application/Onboarding flow.
        if (result.Data.State == PreviewState.NoProfile)
            return RedirectToAction("Index", "Application");

        SetSidebar();

        // S1/PE1: AJAX pagination returns the published-articles fragment; plain GET renders the full page.
        return WantsAjax() ? PartialView("_PreviewResults", result.Data) : View(result.Data);
    }

    private void SetSidebar()
    {
        ViewData["CreatorNav"] = "Preview";
        ViewBag.Sidebar = new CreatorSidebarVm { DisplayName = User.Identity?.Name ?? "Creator" };
    }
}
