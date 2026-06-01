using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Features.Profile.ViewModels;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Features.Profile;

[Area("Accounts")]
[Authorize]
public sealed class ProfileController : BaseController
{
    private readonly ProfileFacade _facade;
    public ProfileController(ProfileFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _facade.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new ProfileVm());
        }
        return View(result.Data);
    }

    [HttpPost("accounts/profile/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateProfileVm vm, CancellationToken ct)
    {
        // The "Details" form POSTs to this action with a flat UpdateProfileVm
        // (its own partial view — see Views/_UpdateProfileForm.cshtml). No
        // prefix-binding trickery is needed: input names are FirstName,
        // LastName, DateOfBirth, Gender, Country, City, AddressLine.
        if (!ModelState.IsValid)
        {
            return await ReloadIndexWithEdit(vm, ct);
        }

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Profile updated.");
            return RedirectToAction(nameof(Index));
        }

        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update profile.");

        return await ReloadIndexWithEdit(vm, ct);
    }

    [HttpPost("accounts/profile/avatar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAvatar(UpdateAvatarVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please choose an image file.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.UpdateAvatarAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Avatar updated.");
            return RedirectToAction(nameof(Index));
        }

        var firstMessage = result.ValidationErrors?.Values
            .SelectMany(messages => messages)
            .FirstOrDefault();

        SetError(firstMessage ?? result.Error ?? "Could not upload avatar.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("accounts/profile/avatar/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        var result = await _facade.DeleteAvatarAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Avatar removed.", "Could not remove avatar.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("accounts/profile/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Your profile has been deleted.");
            return RedirectToAction("Index", "Logout", new { area = "Auth" });
        }

        SetError(result.Error ?? "Could not delete profile.");
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<IActionResult> ReloadIndexWithEdit(UpdateProfileVm edit, CancellationToken ct)
    {
        var load = await _facade.GetAsync(ct);
        if (GuardSignOut(load) is { } signOut) return signOut;

        var vm = load.IsSuccess && load.Data is not null
            ? load.Data
            : new ProfileVm();

        // Preserve the user's in-flight edits.
        return View(nameof(Index), new ProfileVm
        {
            UserId       = vm.UserId,
            FirstName    = vm.FirstName,
            LastName     = vm.LastName,
            DisplayName  = vm.DisplayName,
            AvatarUrl    = vm.AvatarUrl,
            PhoneNumber  = vm.PhoneNumber,
            DateOfBirth  = vm.DateOfBirth,
            Gender       = vm.Gender,
            Country      = vm.Country,
            City         = vm.City,
            AddressLine  = vm.AddressLine,
            Email        = vm.Email,
            Update       = edit,
            UpdateAvatar = new UpdateAvatarVm(),
        });
    }
}
