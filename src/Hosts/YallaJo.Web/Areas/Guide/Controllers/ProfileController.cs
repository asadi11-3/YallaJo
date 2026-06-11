using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Profile;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class ProfileController : GuideBaseController
{
    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

    private readonly GuideProfileFacade _profile;

    public ProfileController(GuideProfileFacade profile) => _profile = profile;

    [HttpGet("guide/profile")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetNav("Profile");
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
        SetNav("Profile");
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

        SetSuccess(L["Guide.Flash.ProfileUpdated"]);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/profile/languages")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLanguage(Guid languageId, string proficiency, CancellationToken ct = default)
    {
        var result = await _profile.AddLanguageAsync(languageId, proficiency, ct);
        return HandleMutation(result, L["Guide.Flash.LanguageAdded"]);
    }

    [HttpPost("guide/profile/languages/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLanguage(Guid languageId, CancellationToken ct = default)
    {
        var result = await _profile.RemoveLanguageAsync(languageId, ct);
        return HandleMutation(result, L["Guide.Flash.LanguageRemoved"]);
    }

    [HttpPost("guide/profile/specializations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSpecialization(Guid specializationId, CancellationToken ct = default)
    {
        var result = await _profile.AddSpecializationAsync(specializationId, ct);
        return HandleMutation(result, L["Guide.Flash.SpecializationAdded"]);
    }

    [HttpPost("guide/profile/specializations/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSpecialization(Guid specializationId, CancellationToken ct = default)
    {
        var result = await _profile.RemoveSpecializationAsync(specializationId, ct);
        return HandleMutation(result, L["Guide.Flash.SpecializationRemoved"]);
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
        return HandleMutation(result, L["Guide.Flash.AvatarUpdated"]);
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
        return HandleMutation(result, L["Guide.Flash.CoverUpdated"]);
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
            SetError(result.Error ?? L["Guide.Flash.DeactivateFailed"].Value);
            return RedirectToAction(nameof(Index));
        }

        // Profile deactivated — the user is no longer an active guide; leave the dashboard.
        SetSuccess(L["Guide.Flash.ProfileDeactivated"]);
        return Redirect("~/accounts");
    }

    private IActionResult HandleMutation(
        YallaJo.Web.Infrastructure.Api.Contracts.ApiResult result, string successMessage)
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

    private bool TryValidateImage(IFormFile? file, out string error)
    {
        if (file is null || file.Length == 0)
        {
            error = L["Guide.Validation.ImageRequired"].Value;
            return false;
        }

        if (file.Length > MaxImageBytes)
        {
            error = L["Guide.Validation.ImageTooLarge"].Value;
            return false;
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            error = L["Guide.Validation.ImageInvalid"].Value;
            return false;
        }

        error = string.Empty;
        return true;
    }
}
