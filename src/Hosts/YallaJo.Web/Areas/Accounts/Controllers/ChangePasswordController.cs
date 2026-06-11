using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Accounts.Models.ChangePassword;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// Phase 2 (Accounts plan): the standalone change-password page was retired and merged into
/// the Settings hub's Security tab. The GET now 301s there; the POST survives unchanged
/// (it is submitted by the Security tab's password form and Profile's inline form) but
/// follows PRG back to the caller via a validated returnUrl (UI-UX-PE1).
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class ChangePasswordController : BaseController
{
    private readonly ChangePasswordFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ChangePasswordController(ChangePasswordFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public IActionResult Index()
        => RedirectPermanent(Url.Action("Index", "Settings", new { area = "Accounts", tab = "security" })!);

    [HttpPost]
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
            : RedirectToAction("Index", "Settings", new { area = "Accounts", tab = "security" });
}
