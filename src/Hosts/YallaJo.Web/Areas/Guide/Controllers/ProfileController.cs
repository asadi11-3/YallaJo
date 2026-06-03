using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class ProfileController : BaseController
{
    private readonly GuideProfileFacade _profile;

    public ProfileController(GuideProfileFacade profile) => _profile = profile;

    [HttpGet("guide/profile")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();
        var result = await _profile.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new GuideProfileVm());
        }

        return View(result.Data);
    }

    [HttpPost("guide/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(EditGuideProfileVm form, Guid guideId, CancellationToken ct = default)
    {
        SetSidebar();

        if (guideId == Guid.Empty)
        {
            SetError("We could not resolve your guide profile. Please reload and try again.");
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return await ReloadAsync(form, ct);
        }

        var result = await _profile.UpdateAsync(guideId, form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
            {
                SetError(result.Error);
            }

            return await ReloadAsync(form, ct);
        }

        SetSuccess("Profile updated.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadAsync(EditGuideProfileVm form, CancellationToken ct)
    {
        var result = await _profile.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        var vm = result is { IsSuccess: true, Data: not null } ? result.Data : new GuideProfileVm();
        vm.Edit = form;
        return View(nameof(Index), vm);
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "Profile";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
