using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Profile;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class ProfileController : BaseController
{
    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

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
            return View(new ProfileVm());
        }

        return View(result.Data);
    }

    [HttpPost("guide/profile/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileFormVm form, CancellationToken ct = default)
    {
        SetSidebar();
        if (!ModelState.IsValid)
        {
            return await ReloadAsync(form, ct);
        }

        var result = await _profile.UpdateProfileAsync(form, ct);
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

    [HttpPost("guide/profile/languages")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLanguage(Guid languageId, string proficiency, CancellationToken ct = default)
    {
        var result = await _profile.AddLanguageAsync(languageId, proficiency, ct);
        return HandleMutation(result, "Language added.");
    }

    [HttpPost("guide/profile/languages/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLanguage(Guid languageId, CancellationToken ct = default)
    {
        var result = await _profile.RemoveLanguageAsync(languageId, ct);
        return HandleMutation(result, "Language removed.");
    }

    [HttpPost("guide/profile/specializations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSpecialization(Guid specializationId, CancellationToken ct = default)
    {
        var result = await _profile.AddSpecializationAsync(specializationId, ct);
        return HandleMutation(result, "Specialization added.");
    }

    [HttpPost("guide/profile/avatar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken ct = default)
    {
        if (!TryValidateImage(file, out var error))
        {
            SetError(error);
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file!.OpenReadStream();
        var result = await _profile.UploadAvatarAsync(stream, file.FileName, file.ContentType, ct);
        return HandleMutation(result, "Avatar updated.");
    }

    [HttpPost("guide/profile/cover")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadCover(IFormFile? file, CancellationToken ct = default)
    {
        if (!TryValidateImage(file, out var error))
        {
            SetError(error);
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file!.OpenReadStream();
        var result = await _profile.UploadCoverAsync(stream, file.FileName, file.ContentType, ct);
        return HandleMutation(result, "Cover image updated.");
    }

    // POST /guide/profile/deactivate — self-deactivate the guide profile (DELETE /guides/me).
    [HttpPost("guide/profile/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TourGuideProfile.DeleteOwn)]
    public async Task<IActionResult> Deactivate(CancellationToken ct = default)
    {
        var result = await _profile.DeactivateAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            SetError(result.Error ?? "Could not deactivate your guide profile.");
            return RedirectToAction(nameof(Index));
        }

        // Profile deactivated — the user is no longer an active guide; leave the dashboard.
        SetSuccess("Your guide profile has been deactivated.");
        return Redirect("~/accounts");
    }

    private IActionResult HandleMutation(
        Infrastructure.Api.Contracts.ApiResult result, string successMessage)
    {
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, successMessage);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadAsync(ProfileFormVm form, CancellationToken ct)
    {
        var result = await _profile.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result is { IsSuccess: true, Data: not null })
        {
            var vm = result.Data;
            vm.Form = form; // preserve user input + surface ModelState errors
            return View(nameof(Index), vm);
        }

        return View(nameof(Index), new ProfileVm { Form = form });
    }

    private static bool TryValidateImage(IFormFile? file, out string error)
    {
        if (file is null || file.Length == 0)
        {
            error = "Please choose an image to upload.";
            return false;
        }

        if (file.Length > MaxImageBytes)
        {
            error = "Image must be 5 MB or smaller.";
            return false;
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            error = "Only image files are allowed.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "Profile";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
