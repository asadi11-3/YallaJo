using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Accounts.Models.ChangePassword;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// Dedicated Change Password page under the Settings area. The GET renders a focused
/// page (account sidebar + settings tabs + password card + security tips/help); the POST
/// reuses the existing <see cref="ChangePasswordFacade"/> and follows PRG back to this page
/// (or a validated returnUrl).
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class ChangePasswordController : BaseController
{
    private readonly ChangePasswordFacade _facade;
    private readonly ProfileFacade _profile;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ChangePasswordController(
        ChangePasswordFacade facade,
        ProfileFacade profile,
        IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _profile = profile;
        _localizer = localizer;
    }

    [HttpGet("accounts/change-password")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AccountNav"] = "ChangePassword";
        ViewData["SettingsActiveTab"] = "security";

        var profileResult = await _profile.GetAsync(ct);
        if (GuardSignOut(profileResult) is { } signOut) return signOut;

        var profile = profileResult.Data;
        ViewBag.Sidebar = new AccountSidebarVm
        {
            AvatarUrl = profile?.AvatarUrl,
            DisplayName = profile?.DisplayName ?? $"{profile?.FirstName} {profile?.LastName}".Trim(),
            Email = profile?.Email
        };

        return View(new ChangePasswordVm());
    }

    [HttpPost("accounts/change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ChangePasswordVm vm, string? returnUrl, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
            SetError(firstError ?? _localizer["Accounts.Msg.PasswordFormError"].Value);
            return RedirectBack(returnUrl);
        }

        var result = await _facade.HandleAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess) SetSuccess(_localizer["Accounts.Msg.PasswordChanged"]);
        else SetError(result.Error ?? _localizer["Accounts.Msg.PasswordChangeFailed"].Value);
        return RedirectBack(returnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
}
