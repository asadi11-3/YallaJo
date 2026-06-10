using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Settings;
using YallaJo.Web.Areas.Accounts.Models.UpdatePhone;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Areas.Auth.Facades;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class SettingsController : BaseController
{
    private readonly SettingsFacade _settings;
    private readonly UpdatePhoneFacade _phone;
    private readonly SessionsFacade _sessions;
    private readonly DevicesFacade _devices;
    private readonly LogoutAllFacade _logoutAll;
    private readonly ProfileFacade _profile;

    public SettingsController(
        SettingsFacade settings,
        UpdatePhoneFacade phone,
        SessionsFacade sessions,
        DevicesFacade devices,
        LogoutAllFacade logoutAll,
        ProfileFacade profile)
    {
        _settings = settings;
        _phone = phone;
        _sessions = sessions;
        _devices = devices;
        _logoutAll = logoutAll;
        _profile = profile;
    }

    // Supported external sign-in providers (canonical names + display labels). Rendered
    // from local constants only — there is no GET-list endpoint for linked providers.
    private static readonly IReadOnlyList<LinkedAccountVm> SupportedProviders =
    [
        new(ExternalProviderConstants.Google, "Google", false),
        new(ExternalProviderConstants.Facebook, "Facebook", false),
    ];

    // Phase 2 (Accounts plan): tab names the settings hub understands. The query value is
    // server-honoured (?tab=) so deep links work without JS (PE1); accounts-settings.js
    // additionally syncs the Bootstrap tabs with the URL hash.
    private static string NormalizeTab(string? tab) => tab?.ToLowerInvariant() switch
    {
        "security" => "security",
        "devices" => "devices",
        "linked" => "linked",
        "privacy" => "privacy",
        "account" => "account",
        _ => "notifications",
    };

    [HttpGet]
    public async Task<IActionResult> Index(string? tab, CancellationToken ct)
    {
        ViewData["AccountNav"] = "Settings";
        ViewData["SettingsTab"] = NormalizeTab(tab);

        var rowsResult = await _settings.GetNotificationRowsAsync(ct);
        if (GuardSignOut(rowsResult) is { } so1) return so1;

        var marketingResult = await _settings.GetMarketingAsync(ct);
        if (GuardSignOut(marketingResult) is { } so2) return so2;

        var sessionsResult = await _sessions.GetSessionsAsync(ct);
        if (GuardSignOut(sessionsResult) is { } so3) return so3;

        var profileResult = await _profile.GetAsync(ct);
        if (GuardSignOut(profileResult) is { } so4) return so4;

        // §3.10 Devices tab — best-effort list (degrades to empty, never blocks the page).
        var devices = await _devices.GetTokensAsync(ct);

        var profile = profileResult.Data;
        ViewBag.Sidebar = new AccountSidebarVm
        {
            AvatarUrl = profile?.AvatarUrl,
            DisplayName = profile?.DisplayName
                ?? $"{profile?.FirstName} {profile?.LastName}".Trim(),
            Email = profile?.Email
        };

        if (!rowsResult.IsSuccess) SetError(rowsResult.Error);

        var vm = new SettingsVm
        {
            NotificationRows = rowsResult.Data ?? [],
            Marketing = marketingResult.Data ?? new MarketingConsentVm(),
            PhoneNumber = profile?.PhoneNumber,
            Sessions = sessionsResult.Data?.Sessions ?? [],
            Devices = devices,
            LinkedAccounts = SupportedProviders,
        };

        return View(vm);
    }

    [HttpPost("accounts/settings/notifications")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNotifications(IFormCollection form, CancellationToken ct)
    {
        var enabledKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in form.Keys)
        {
            if (!key.StartsWith("pref_", StringComparison.Ordinal)) continue;
            // key shape: pref_{Type}_{Channel}
            var rest = key["pref_".Length..];
            var lastUnderscore = rest.LastIndexOf('_');
            if (lastUnderscore <= 0) continue;
            var type = rest[..lastUnderscore];
            var channel = rest[(lastUnderscore + 1)..];
            var value = form[key].ToString();
            var isOn = value.Contains("true", StringComparison.OrdinalIgnoreCase)
                || value == "on";
            if (isOn) enabledKeys.Add($"{type}|{channel}");
        }

        var result = await _settings.UpdateNotificationsAsync(enabledKeys, ct);
        if (GuardSignOut(result) is { } so) return so;
        if (result.IsSuccess) SetSuccess("Notification preferences saved.");
        else SetError(result.Error);
        return RedirectToAction(nameof(Index), new { tab = "notifications" });
    }

    [HttpPost("accounts/settings/marketing")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMarketing(
        bool emailDigest,
        bool pushNotifications,
        bool reEngagementCampaigns,
        CancellationToken ct)
    {
        var result = await _settings.UpdateMarketingAsync(
            emailDigest, pushNotifications, reEngagementCampaigns, ct);
        if (GuardSignOut(result) is { } so) return so;
        if (result.IsSuccess) SetSuccess("Marketing preferences saved.");
        else SetError(result.Error);
        return RedirectToAction(nameof(Index), new { tab = "notifications" });
    }

    [HttpPost("accounts/settings/phone")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePhone(UpdatePhoneVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please enter a valid phone number.");
            return RedirectToAction(nameof(Index), new { tab = "security" });
        }

        var result = await _phone.HandleAsync(vm, ct);
        if (GuardSignOut(result) is { } so) return so;
        if (result.IsSuccess) SetSuccess("Phone number updated.");
        else SetError(result.Error);
        return RedirectToAction(nameof(Index), new { tab = "security" });
    }

    [HttpPost("accounts/settings/sessions/revoke/{sessionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        var result = await _sessions.RevokeAsync(sessionId, ct);
        if (GuardSignOut(result) is { } so) return so;
        if (result.IsSuccess) SetSuccess("Session signed out.");
        else SetError(result.Error);
        return RedirectToAction(nameof(Index), new { tab = "security" });
    }

    [HttpPost("accounts/settings/devices/trust/{deviceId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrustDevice(Guid deviceId, CancellationToken ct)
    {
        var result = await _devices.TrustAsync(deviceId, ct);
        if (GuardSignOut(result) is { } so) return so;
        if (result.IsSuccess) SetSuccess("Device marked as trusted.");
        else SetError(result.Error);
        return RedirectToAction(nameof(Index), new { tab = "security" });
    }

    [HttpPost("accounts/settings/logout-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var result = await _logoutAll.HandleAsync(ct);
        if (!result.IsSuccess) SetError(result.Error);
        return RedirectToAction("SignIn", "Auth", new { area = "Auth" });
    }

    // ── §3.10 Devices tab — register / remove push-notification device tokens ────

    [HttpPost("accounts/settings/devices")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.DeviceToken.Create)]
    public async Task<IActionResult> RegisterDevice(
        string deviceId, string platform, string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(token)
            || string.IsNullOrWhiteSpace(platform))
        {
            SetError("Device id, platform and token are all required.");
            return RedirectToAction(nameof(Index), new { tab = "devices" });
        }

        var result = await _devices.RegisterTokenAsync(deviceId, platform, token, ct);
        if (GuardSignOut(result) is { } so) return so;
        if (result.IsSuccess) SetSuccess("Device registered for notifications.");
        else SetError(result.Error);
        return RedirectToAction(nameof(Index), new { tab = "devices" });
    }

    [HttpPost("accounts/settings/devices/{id:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.DeviceToken.Delete)]
    public async Task<IActionResult> RemoveDevice(Guid id, CancellationToken ct)
    {
        var result = await _devices.DeleteTokenAsync(id, ct);
        if (GuardSignOut(result) is { } so) return so;
        if (result.IsSuccess) SetSuccess("Device removed.");
        else SetError(result.Error);
        return RedirectToAction(nameof(Index), new { tab = "devices" });
    }
}
