using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Privacy;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// FE-1D — authenticated Privacy &amp; Data controls (GDPR data rights over the user's
/// personal analytics/recommendation data): export, request deletion (30-day window),
/// and cancel a pending deletion.
/// <para>
/// This page is intentionally separate from Settings and from the account-deletion
/// ("Delete Profile") page. The destructive delete-data action deletes recommendation/
/// analytics data only — NOT the account — and requires an explicit typed confirmation.
/// </para>
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class PrivacyController : BaseController
{
    private const string ConfirmationWord = "DELETE";

    private readonly PrivacyFacade _privacy;
    private readonly ProfileFacade _profile;

    public PrivacyController(PrivacyFacade privacy, ProfileFacade profile)
    {
        _privacy = privacy;
        _profile = profile;
    }

    [HttpGet("accounts/privacy")]
    [RequirePermission(WebPermission.Preference.Read)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AccountNav"] = "Privacy";
        await PopulateSidebarAsync(ct);
        return View(new PrivacyVm());
    }

    [HttpGet("accounts/privacy/export")]
    [RequirePermission(WebPermission.Preference.Read)]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var result = await _privacy.ExportAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? "Could not prepare your data export.");
            return RedirectToAction(nameof(Index));
        }

        // Re-serve the proxied JSON bytes as a download. JWT never leaves the server.
        return File(result.Data.Content, "application/json", "my-data-export.json");
    }

    [HttpPost("accounts/privacy/delete-data")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Preference.Update)]
    public async Task<IActionResult> DeleteData(string? confirmation, CancellationToken ct)
    {
        // Destructive: require the explicit typed confirmation word before proceeding.
        if (!string.Equals(confirmation?.Trim(), ConfirmationWord, StringComparison.Ordinal))
        {
            SetError($"Please type {ConfirmationWord} to confirm deleting your recommendation data.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _privacy.RequestDataDeletionAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your recommendation data is scheduled for deletion. You can cancel within 30 days.");
        else
            SetError(result.Error ?? "Could not request data deletion.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("accounts/privacy/cancel-deletion")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Preference.Update)]
    public async Task<IActionResult> CancelDeletion(CancellationToken ct)
    {
        var result = await _privacy.CancelDataDeletionAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your data deletion has been cancelled.");
        else
            SetError(result.Error ?? "Could not cancel the deletion.");

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
