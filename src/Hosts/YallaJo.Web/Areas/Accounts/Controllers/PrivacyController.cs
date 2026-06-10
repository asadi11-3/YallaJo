using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// FE-1D — authenticated Privacy &amp; Data controls (GDPR data rights over the user's
/// personal analytics/recommendation data): export, request deletion (30-day window),
/// and cancel a pending deletion.
/// <para>
/// Phase 2 (Accounts plan): the standalone privacy page was retired and merged into the
/// Settings hub's Privacy &amp; data tab — the GET now 301s there (no permission gate on a
/// pure redirect; the actions below keep their gates). The destructive delete-data action
/// deletes recommendation/analytics data only — NOT the account — and requires an explicit
/// typed confirmation.
/// </para>
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class PrivacyController : BaseController
{
    private const string ConfirmationWord = "DELETE";

    private readonly PrivacyFacade _privacy;

    public PrivacyController(PrivacyFacade privacy) => _privacy = privacy;

    [HttpGet("accounts/privacy")]
    public IActionResult Index()
        => RedirectPermanent(Url.Action("Index", "Settings", new { area = "Accounts", tab = "privacy" })!);

    [HttpGet("accounts/privacy/export")]
    [RequirePermission(WebPermission.Preference.Read)]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var result = await _privacy.ExportAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? "Could not prepare your data export.");
            return BackToTab();
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
            return BackToTab();
        }

        var result = await _privacy.RequestDataDeletionAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your recommendation data is scheduled for deletion. You can cancel within 30 days.");
        else
            SetError(result.Error ?? "Could not request data deletion.");

        return BackToTab();
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

        return BackToTab();
    }

    private IActionResult BackToTab()
        => RedirectToAction("Index", "Settings", new { area = "Accounts", tab = "privacy" });
}
