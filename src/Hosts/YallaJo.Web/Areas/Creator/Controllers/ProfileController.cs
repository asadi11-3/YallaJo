using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Creator.Facades;
using YallaJo.Web.Areas.Creator.Models.Profile;
using YallaJo.Web.Areas.Creator.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Creator.Controllers;

/// <summary>
/// Creator profile self-service (CCD-3).
/// <para>Permission gates: view = <c>Creator.Read</c>; update profile/avatar =
/// <c>Creator.Update</c>; self-deactivate = <c>Creator.Delete</c>. Separate POST
/// routes per action so each carries the correct gate.</para>
/// <para>Avatar accepts a managed file upload (posted to the ContentBlogs managed
/// avatar endpoint, which validates/stores/persists in one round-trip) and a remove
/// action (managed clear endpoint). A legacy URL-only action remains for back-compat
/// but is no longer surfaced in the UI. All gate on <c>Creator.Update</c>.</para>
/// </summary>
[Area("Creator")]
[Authorize]
public sealed class ProfileController : BaseController
{
    private readonly CreatorProfileFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ProfileController(CreatorProfileFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    // GET /creator/profile
    [HttpGet("creator/profile")]
    [RequirePermission(WebPermission.Creator.Read)]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var result = await _facade.GetProfileAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            SetSidebar(null);
            return View(new CreatorProfileVm());
        }

        // No creator profile yet → send the user to the Application/Onboarding flow.
        if (!result.Data.HasProfile)
            return RedirectToAction("Index", "Application");

        SetSidebar(result.Data.AvatarUrl);
        return View(result.Data);
    }

    // POST /creator/profile  — Creator.Update
    [HttpPost("creator/profile")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Update)]
    public async Task<IActionResult> Update(CreatorProfileVm form, CancellationToken ct = default)
    {
        SetSidebar(form.AvatarUrl);

        if (!ModelState.IsValid)
            return ReeditView(form);

        var result = await _facade.UpdateProfileAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            // 409 (slug taken) and other non-validation errors render inline.
            if (!ApplyValidationErrors(result))
                ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Creator.Profile.Flash.UpdateFailed"].Value);
            return ReeditView(form);
        }

        SetSuccess(_localizer["Creator.Profile.Flash.Updated"]);
        return RedirectToAction(nameof(Index));
    }

    // POST /creator/profile/avatar  — Creator.Update
    [HttpPost("creator/profile/avatar")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Update)]
    public async Task<IActionResult> Avatar(UpdateAvatarVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError(FirstModelError() ?? _localizer["Creator.Profile.Flash.AvatarUrlInvalid"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.UpdateAvatarAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Profile.Flash.AvatarUpdated"].Value, _localizer["Creator.Profile.Flash.AvatarFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    // POST /creator/profile/avatar/upload  — Creator.Update (managed file upload)
    [HttpPost("creator/profile/avatar/upload")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Update)]
    public async Task<IActionResult> AvatarUpload(IFormFile? avatarFile, CancellationToken ct = default)
    {
        if (avatarFile is null)
        {
            SetError(_localizer["Creator.Profile.Flash.ChooseImage"]);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.UploadAvatarFileAsync(avatarFile, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Profile.Flash.AvatarUpdated"].Value, _localizer["Creator.Profile.Flash.AvatarFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    // POST /creator/profile/avatar/remove  — Creator.Update (managed clear endpoint)
    [HttpPost("creator/profile/avatar/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Update)]
    public async Task<IActionResult> RemoveAvatar(CancellationToken ct = default)
    {
        var result = await _facade.RemoveAvatarAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Profile.Flash.AvatarRemoved"].Value, _localizer["Creator.Profile.Flash.AvatarFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    // POST /creator/profile/deactivate  — Creator.Delete (modal-confirmed)
    [HttpPost("creator/profile/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Creator.Delete)]
    public async Task<IActionResult> Deactivate(bool confirm, CancellationToken ct = default)
    {
        if (!confirm)
        {
            SetError(_localizer["Creator.Profile.Flash.ConfirmDeactivate"]);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.SelfDeactivateAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result,
            _localizer["Creator.Profile.Flash.Deactivated"].Value,
            _localizer["Creator.Profile.Flash.DeactivateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Re-renders the editable profile form after a failed POST. The posted VM does
    /// not carry Status/CurrentSlug (they're display-only), so we force the editable
    /// branch (Status = "Active") and restore the current slug for the form hint.
    /// </summary>
    private IActionResult ReeditView(CreatorProfileVm form)
    {
        form.Status = "Active";
        if (string.IsNullOrWhiteSpace(form.CurrentSlug))
            form.CurrentSlug = form.NewSlug ?? string.Empty;
        return View(nameof(Index), form);
    }

    private void SetSidebar(string? avatarUrl)
    {
        ViewData["CreatorNav"] = "Profile";
        ViewBag.Sidebar = new CreatorSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Creator",
            AvatarUrl = avatarUrl,
        };
    }

    private string? FirstModelError() =>
        ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
}
