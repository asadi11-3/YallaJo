using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Creator.Facades;
using YallaJo.Web.Areas.Creator.Models.Profile;
using YallaJo.Web.Areas.Creator.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Creator.Controllers;

/// <summary>
/// Creator profile self-service (CCD-3).
/// <para>Permission gates: view = <c>Creator.Read</c>; update profile/avatar =
/// <c>Creator.Update</c>; self-deactivate = <c>Creator.Delete</c>. Separate POST
/// routes per action so each carries the correct gate.</para>
/// <para>Avatar is URL-only (no Creator/Profile EntityType → no file upload).</para>
/// </summary>
[Area("Creator")]
[Authorize]
public sealed class ProfileController : BaseController
{
    private readonly CreatorProfileFacade _facade;

    public ProfileController(CreatorProfileFacade facade) => _facade = facade;

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
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not update your profile.");
            return ReeditView(form);
        }

        SetSuccess("Your creator profile was updated.");
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
            SetError(FirstModelError() ?? "Please enter a valid avatar URL.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.UpdateAvatarAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Your avatar was updated.", "Could not update your avatar.");
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
            SetError("Please confirm that you want to deactivate your creator profile.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.SelfDeactivateAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result,
            "Your creator profile has been deactivated. You have 60 days to reactivate before it is permanently removed.",
            "Could not deactivate your creator profile.");
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
