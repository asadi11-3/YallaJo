using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.ChangePassword;
using YallaJo.Web.Areas.Accounts.Models.Profile;
using YallaJo.Web.Areas.Accounts.Models.UpdatePhone;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models;
using YallaJo.Web.Areas.Provider.Models.Settings;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class SettingsController : BaseController
{
    private readonly ProfileFacade _profile;
    private readonly ChangePasswordFacade _password;
    private readonly UpdatePhoneFacade _phone;
    private readonly ProviderFacade _provider;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ProfileFacade profile,
        ChangePasswordFacade password,
        UpdatePhoneFacade phone,
        ProviderFacade provider,
        ILogger<SettingsController> logger)
    {
        _profile = profile;
        _password = password;
        _phone = phone;
        _provider = provider;
        _logger = logger;
    }

    [HttpGet("provider/settings")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {

        var result = await _profile.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ProviderSettingsVm());
        }

        var business = await SafeBusinessAsync(ct);
        return View(BuildVm(result.Data, business));
    }

    [HttpPost("provider/settings/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateProfileVm form, CancellationToken ct = default)
    {

        if (!ModelState.IsValid)
            return await ReloadAsync(profile: form, ct: ct);

        var result = await _profile.UpdateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result)) SetError(result.Error);
            return await ReloadAsync(profile: form, ct: ct);
        }

        SetSuccess(L["Provider.Flash.ProfileUpdated"]);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("provider/settings/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVm form, CancellationToken ct = default)
    {

        if (!ModelState.IsValid)
            return await ReloadAsync(password: form, ct: ct);

        var result = await _password.HandleAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result)) SetError(result.Error);
            return await ReloadAsync(password: form, ct: ct);
        }

        SetSuccess(L["Provider.Flash.PasswordChanged"]);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("provider/settings/phone")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePhone(UpdatePhoneVm form, CancellationToken ct = default)
    {

        if (!ModelState.IsValid)
            return await ReloadAsync(phone: form, ct: ct);

        var result = await _phone.HandleAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result)) SetError(result.Error);
            return await ReloadAsync(phone: form, ct: ct);
        }

        SetSuccess(L["Provider.Flash.PhoneUpdated"]);
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

        var business = await SafeBusinessAsync(ct);
        var vm = result.IsSuccess && result.Data is not null
            ? BuildVm(result.Data, business)
            : new ProviderSettingsVm { Business = business };

        if (profile is not null) vm.Profile = profile;
        if (password is not null) vm.Password = password;
        if (phone is not null) vm.Phone = phone;

        return View(nameof(Index), vm);
    }

    private static ProviderSettingsVm BuildVm(ProfileVm p, ProviderBusinessInfoVm? business) => new()
    {
        Email = p.Email,
        DisplayName = string.IsNullOrWhiteSpace(p.DisplayName) ? $"{p.FirstName} {p.LastName}".Trim() : p.DisplayName,
        AvatarUrl = p.AvatarUrl,
        PhoneNumber = p.PhoneNumber,
        Profile = p.Update,
        Phone = new UpdatePhoneVm { PhoneNumber = p.PhoneNumber ?? string.Empty },
        Business = business,
    };

    /// <summary>
    /// Loads the provider business information via <see cref="ProviderFacade.GetSettingsAsync"/>.
    /// Non-blocking: returns <c>null</c> on any failure (e.g. no application yet) so the
    /// account-settings forms always render.
    /// </summary>
    private async Task<ProviderBusinessInfoVm?> SafeBusinessAsync(CancellationToken ct)
    {
        try
        {
            var result = await _provider.GetSettingsAsync(ct);
            if (!result.IsSuccess || result.Data is null) return null;

            var d = result.Data;
            return new ProviderBusinessInfoVm
            {
                BusinessName = d.BusinessName,
                ContactEmail = d.ContactEmail,
                ContactPhone = d.ContactPhone,
                Address = d.Address,
                Description = d.Description,
                ProviderType = ProviderTypeLabel(d.ProviderType),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load provider business settings");
            return null;
        }
    }

    // Mirrors Accounts.Domain.Enums.ProviderType (byte). The API serializes the enum
    // as its numeric value for this client (no JsonStringEnumConverter configured).
    // Labels come from resx (CON1) so the settings page localizes in AR.
    private string ProviderTypeLabel(int value) => value switch
    {
        0 => L["Provider.Settings.Type.TourOperator"].Value,
        1 => L["Provider.Settings.Type.IndependentGuide"].Value,
        2 => L["Provider.Settings.Type.HotelResort"].Value,
        3 => L["Provider.Settings.Type.ActivityCenter"].Value,
        4 => L["Provider.Settings.Type.Agency"].Value,
        5 => L["Provider.Settings.Type.BusinessOwner"].Value,
        _ => L["Provider.Settings.Type.Unknown"].Value,
    };

}
