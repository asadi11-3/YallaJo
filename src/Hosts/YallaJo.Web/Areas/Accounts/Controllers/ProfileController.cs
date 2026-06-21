using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Accounts.Models.Profile;
using YallaJo.Web.Areas.Accounts.Models.Promo;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class ProfileController : BaseController
{
    private readonly ProfileFacade _facade;
    private readonly PromoFacade _promoFacade;
    private readonly ICurrentUser _currentUser;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ProfileController(
        ProfileFacade facade,
        PromoFacade promoFacade,
        ICurrentUser currentUser,
        IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _promoFacade = promoFacade;
        _currentUser = currentUser;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _facade.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        var promos = await LoadPromosAsync(ct);

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return View(BuildVm(new ProfileVm(), promos));
        }
        return View(BuildVm(result.Data!, promos));
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
            // AJAX: return the refreshed card with inline validation errors (400);
            // non-JS: re-render the full page preserving edits (PRG fallback).
            if (WantsAjax()) return await ProfileCardPartialWithEdit(vm, ct, 400);
            return await ReloadIndexWithEdit(vm, ct);
        }

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            if (WantsAjax())
            {
                var refreshed = await _facade.GetAsync(ct);
                if (GuardSignOut(refreshed) is { } signOutAfter) return signOutAfter;
                var promos = await LoadPromosAsync(ct);
                var src = refreshed.IsSuccess && refreshed.Data is not null
                    ? refreshed.Data
                    : new ProfileVm();
                return PartialView("_ProfileCard", BuildVm(src, promos));
            }

            SetSuccess(_localizer["Accounts.Msg.ProfileUpdated"]);
            return RedirectToAction(nameof(Index));
        }

        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Accounts.Msg.ProfileUpdateFailed"].Value);

        if (WantsAjax())
        {
            // Validation failures → swap the card (400 html). Other failures → JSON error toast.
            return ModelState.IsValid
                ? BadRequest(new { error = result.Error ?? _localizer["Accounts.Msg.ProfileUpdateFailed"].Value })
                : await ProfileCardPartialWithEdit(vm, ct, 400);
        }

        return await ReloadIndexWithEdit(vm, ct);
    }

    [HttpPost("accounts/profile/avatar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAvatar(UpdateAvatarVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // AJAX: surface a visible error (JSON), no false success; non-JS: PRG.
            if (WantsAjax())
                return BadRequest(new { error = _localizer["Accounts.Msg.ChooseImage"].Value });

            SetError(_localizer["Accounts.Msg.ChooseImage"]);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.UpdateAvatarAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            // AJAX: refresh the profile card (carries the new avatar URL via re-fetch);
            // non-JS: PRG with a success flash.
            if (WantsAjax()) return await ProfileCardPartialAsync(ct);

            SetSuccess(_localizer["Accounts.Msg.AvatarUpdated"]);
            return RedirectToAction(nameof(Index));
        }

        var firstMessage = result.ValidationErrors?.Values
            .SelectMany(messages => messages)
            .FirstOrDefault();

        var uploadError = firstMessage ?? result.Error ?? _localizer["Accounts.Msg.AvatarUploadFailed"].Value;

        if (WantsAjax()) return BadRequest(new { error = uploadError });

        SetError(uploadError);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("accounts/profile/avatar/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        var result = await _facade.DeleteAvatarAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            if (WantsAjax()) return await ProfileCardPartialAsync(ct);

            SetSuccess(_localizer["Accounts.Msg.AvatarRemoved"]);
            return RedirectToAction(nameof(Index));
        }

        var removeError = result.Error ?? _localizer["Accounts.Msg.AvatarRemoveFailed"].Value;

        if (WantsAjax()) return BadRequest(new { error = removeError });

        SetError(removeError);
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
            SetSuccess(_localizer["Accounts.Msg.ProfileDeleted"]);
            return RedirectToAction("SignIn", "Auth", new { area = "Auth" });
        }

        SetError(result.Error ?? _localizer["Accounts.Msg.ProfileDeleteFailed"].Value);
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

        var promos = await LoadPromosAsync(ct);

        // Preserve the user's in-flight edits.
        return View(nameof(Index), BuildVm(vm, promos, edit));
    }

    private async Task<IActionResult> ProfileCardPartialWithEdit(UpdateProfileVm edit, CancellationToken ct, int statusCode)
    {
        var load = await _facade.GetAsync(ct);
        if (GuardSignOut(load) is { } signOut) return signOut;

        var vm = load.IsSuccess && load.Data is not null
            ? load.Data
            : new ProfileVm();

        var promos = await LoadPromosAsync(ct);

        Response.StatusCode = statusCode;
        return PartialView("_ProfileCard", BuildVm(vm, promos, edit));
    }

    // Re-fetch the profile and return the refreshed _ProfileCard fragment (200). Used by
    // the avatar AJAX flows: the write endpoints return no avatar URL, so re-fetching the
    // profile yields the resolved AvatarUrl which the swapped-in card then carries.
    private async Task<IActionResult> ProfileCardPartialAsync(CancellationToken ct)
    {
        var load = await _facade.GetAsync(ct);
        if (GuardSignOut(load) is { } signOut) return signOut;

        var vm = load.IsSuccess && load.Data is not null
            ? load.Data
            : new ProfileVm();

        var promos = await LoadPromosAsync(ct);
        return PartialView("_ProfileCard", BuildVm(vm, promos));
    }

    private async Task<IReadOnlyList<PromoBlockVm>> LoadPromosAsync(CancellationToken ct)
    {
        var isAdmin = _currentUser.IsInRole("Admin")
            || _currentUser.IsInRole("SuperAdmin")
            || _currentUser.IsInRole("Owner");

        var result = await _promoFacade.GetForProfileAsync(includeInactive: isAdmin, ct);
        return result.IsSuccess && result.Data is not null
            ? result.Data
            : Array.Empty<PromoBlockVm>();
    }

    private static ProfileVm BuildVm(
        ProfileVm src,
        IReadOnlyList<PromoBlockVm> promos,
        UpdateProfileVm? edit = null,
        UpdateAvatarVm? avatar = null) => new()
        {
            UserId       = src.UserId,
            FirstName    = src.FirstName,
            LastName     = src.LastName,
            DisplayName  = src.DisplayName,
            AvatarUrl    = src.AvatarUrl,
            PhoneNumber  = src.PhoneNumber,
            DateOfBirth  = src.DateOfBirth,
            Gender       = src.Gender,
            Country      = src.Country,
            City         = src.City,
            AddressLine  = src.AddressLine,
            Email        = src.Email,
            Update       = edit ?? src.Update,
            UpdateAvatar = avatar ?? new UpdateAvatarVm(),
            Promos       = promos,
        };
}
