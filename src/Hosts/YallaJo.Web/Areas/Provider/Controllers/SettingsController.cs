using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.ChangePassword;
using YallaJo.Web.Areas.Accounts.Models.Profile;
using YallaJo.Web.Areas.Accounts.Models.UpdatePhone;
using YallaJo.Web.Areas.Provider.Models.Settings;
using YallaJo.Web.Areas.Provider.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class SettingsController : BaseController
{
    private readonly ProfileFacade _profile;
    private readonly ChangePasswordFacade _password;
    private readonly UpdatePhoneFacade _phone;

    public SettingsController(ProfileFacade profile, ChangePasswordFacade password, UpdatePhoneFacade phone)
    {
        _profile = profile;
        _password = password;
        _phone = phone;
    }

    [HttpGet("provider/settings")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();

        var result = await _profile.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ProviderSettingsVm());
        }

        return View(BuildVm(result.Data));
    }

    [HttpPost("provider/settings/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateProfileVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (!ModelState.IsValid)
            return await ReloadAsync(profile: form, ct: ct);

        var result = await _profile.UpdateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result)) SetError(result.Error);
            return await ReloadAsync(profile: form, ct: ct);
        }

        SetSuccess("Your profile has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("provider/settings/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (!ModelState.IsValid)
            return await ReloadAsync(password: form, ct: ct);

        var result = await _password.HandleAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result)) SetError(result.Error);
            return await ReloadAsync(password: form, ct: ct);
        }

        SetSuccess("Your password has been changed.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("provider/settings/phone")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePhone(UpdatePhoneVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (!ModelState.IsValid)
            return await ReloadAsync(phone: form, ct: ct);

        var result = await _phone.HandleAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result)) SetError(result.Error);
            return await ReloadAsync(phone: form, ct: ct);
        }

        SetSuccess("Your phone number has been updated.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadAsync(
        UpdateProfileVm? profile = null,
        ChangePasswordVm? password = null,
        UpdatePhoneVm? phone = null,
        CancellationToken ct = default)
    {
        var result = await _profile.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        var vm = result.IsSuccess && result.Data is not null
            ? BuildVm(result.Data)
            : new ProviderSettingsVm();

        if (profile is not null) vm.Profile = profile;
        if (password is not null) vm.Password = password;
        if (phone is not null) vm.Phone = phone;

        return View(nameof(Index), vm);
    }

    private static ProviderSettingsVm BuildVm(ProfileVm p) => new()
    {
        Email = p.Email,
        DisplayName = string.IsNullOrWhiteSpace(p.DisplayName) ? $"{p.FirstName} {p.LastName}".Trim() : p.DisplayName,
        AvatarUrl = p.AvatarUrl,
        PhoneNumber = p.PhoneNumber,
        Profile = p.Update,
        Phone = new UpdatePhoneVm { PhoneNumber = p.PhoneNumber ?? string.Empty },
    };

    private void SetSidebar()
    {
        ViewData["ProviderNav"] = "Settings";
        ViewBag.Sidebar = new ProviderSidebarVm { DisplayName = User.Identity?.Name ?? "Provider" };
    }
}
