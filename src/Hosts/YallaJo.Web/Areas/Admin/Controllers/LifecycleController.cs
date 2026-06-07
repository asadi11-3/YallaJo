using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Lifecycle;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

/// <summary>
/// Phase 5B — admin lifecycle action endpoints invoked by the inline
/// forms on the User Details page. Each action posts to a verb-named
/// route, runs the matching API call via <see cref="LifecycleFacade"/>,
/// then redirects back to <c>Users/Details/{id}</c> with a TempData
/// success or error message.
/// <para>
/// All actions require <c>User.UpdateAny</c> — same gate as the
/// existing legacy Activate/Deactivate. Backend authorization
/// (hierarchy / self-management / lifecycle eligibility) remains the
/// single source of truth; the UI surfaces 403/409 messages produced
/// by the API.
/// </para>
/// </summary>
[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.User.UpdateAny)]
public sealed class LifecycleController : Controller
{
    private readonly LifecycleFacade _facade;
    public LifecycleController(LifecycleFacade facade) => _facade = facade;

    [HttpPost("admin/users/{userId:guid}/suspend")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(Guid userId, CancellationToken ct)
    {
        var result = await _facade.SuspendAsync(userId, ct);
        return RedirectAfter(userId, result, "User suspended.");
    }

    [HttpPost("admin/users/{userId:guid}/reactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(Guid userId, CancellationToken ct)
    {
        var result = await _facade.ReactivateAsync(userId, ct);
        return RedirectAfter(userId, result, "User reactivated.");
    }

    [HttpPost("admin/users/{userId:guid}/archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(
        Guid userId, AdminArchiveVm vm, CancellationToken ct)
    {
        // Phase 5C — server-side guard for the typed-ARCHIVE
        // confirmation. The Archive modal's JS-disabled-button gate is
        // cosmetic; this validation is authoritative.
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? "Type ARCHIVE in capitals to confirm.";
            TempData["Error"] = firstError;
            return RedirectToDetails(userId);
        }

        var result = await _facade.ArchiveAsync(userId, ct);
        return RedirectAfter(userId, result, "User archived.");
    }

    [HttpPost("admin/users/{userId:guid}/reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        Guid userId, AdminResetPasswordVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Reason is too long (max 500 characters).";
            return RedirectToDetails(userId);
        }

        var result = await _facade.ResetPasswordAsync(userId, vm, ct);
        return RedirectAfter(userId, result, "Password reset email queued.");
    }

    [HttpPost("admin/users/{userId:guid}/reassign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(
        Guid userId, AdminReassignAccountVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // Phase 5B — no modal to reopen; collapse the field-level
            // errors to a single TempData message so the user sees what
            // failed without dropping the typed values.
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                ?? "Reassignment input is invalid.";
            TempData["Error"] = firstError;
            return RedirectToDetails(userId);
        }

        var result = await _facade.ReassignAsync(userId, vm, ct);
        return RedirectAfter(userId, result, "Account reassigned. Activation email queued.");
    }

    // §8.13 — force-revoke all active sessions for a user.
    [HttpPost("admin/users/{userId:guid}/revoke-sessions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSessions(Guid userId, CancellationToken ct)
    {
        var result = await _facade.RevokeSessionsAsync(userId, ct);
        return RedirectAfter(userId, result, "All sessions revoked. The user will need to sign in again.");
    }

    private IActionResult RedirectAfter(Guid userId, Infrastructure.Api.Contracts.ApiResult result, string successMessage)
    {
        if (result.RequireSignOut)
            return RedirectToAction("SignIn", "Auth", new { area = "Auth" });

        if (result.IsSuccess)
        {
            TempData["Success"] = successMessage;
        }
        else if (result.ValidationErrors is not null)
        {
            // No view re-render in Phase 5B — collapse to a single
            // human-readable error; modals in Phase 5C will keep the
            // typed values and re-render field-level messages.
            var first = result.ValidationErrors
                .SelectMany(kv => kv.Value)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
            TempData["Error"] = first ?? "The action could not be completed.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "The action could not be completed.";
        }

        return RedirectToDetails(userId);
    }

    private IActionResult RedirectToDetails(Guid userId) =>
        RedirectToAction("Details", "Users", new { area = "Admin", userId });
}
