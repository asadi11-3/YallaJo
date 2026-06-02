using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class DeleteController : BaseController
{
    private readonly DeleteFacade _delete;
    private readonly ProfileFacade _profile;

    public DeleteController(DeleteFacade delete, ProfileFacade profile)
    {
        _delete = delete;
        _profile = profile;
    }

    [HttpGet("accounts/delete")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AccountNav"] = "Delete";
        await PopulateSidebarAsync(ct);
        return View();
    }

    [HttpPost("accounts/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        var result = await _delete.DeleteAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        // Account soft-deleted and local cookie cleared; send the user to sign-in.
        SetSuccess("Your account has been deleted. You can restore it by signing in again.");
        return RedirectToAction("SignIn", "Auth", new { area = "Auth" });
    }

    [HttpPost("accounts/delete/restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(CancellationToken ct)
    {
        var result = await _delete.RestoreAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your account has been restored.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSidebarAsync(CancellationToken ct)
    {
        var profile = await _profile.GetAsync(ct);
        if (profile is { IsSuccess: true, Data: { } p })
        {
            ViewBag.Sidebar = new AccountSidebarVm
            {
                AvatarUrl = p.AvatarUrl,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName)
                    ? $"{p.FirstName} {p.LastName}".Trim()
                    : p.DisplayName,
                Email = p.Email,
            };
        }
        else
        {
            ViewBag.Sidebar = new AccountSidebarVm();
        }
    }
}
