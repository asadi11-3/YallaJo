using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// Phase 2 (Accounts plan): the standalone delete-profile page was retired and merged into
/// the Settings hub's Account tab. The GET 301s there; the Delete/Restore POSTs keep their
/// routes and behaviour, with PRG back to the Account tab.
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class DeleteController : BaseController
{
    private readonly DeleteFacade _delete;

    public DeleteController(DeleteFacade delete) => _delete = delete;

    [HttpGet("accounts/delete")]
    public IActionResult Index()
        => RedirectPermanent(Url.Action("Index", "Settings", new { area = "Accounts", tab = "account" })!);

    [HttpPost("accounts/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        var result = await _delete.DeleteAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return BackToTab();
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

        return BackToTab();
    }

    private IActionResult BackToTab()
        => RedirectToAction("Index", "Settings", new { area = "Accounts", tab = "account" });
}
