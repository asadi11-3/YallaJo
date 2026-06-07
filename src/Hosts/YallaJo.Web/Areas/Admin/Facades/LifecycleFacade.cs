using YallaJo.Web.Areas.Admin.Models.Lifecycle;
using YallaJo.Web.Areas.Admin.Models.Lifecycle;
using YallaJo.Web.Infrastructure.Api.Contracts;

using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Users;
namespace YallaJo.Web.Areas.Admin.Facades;

/// <summary>
/// Phase 5B — orchestrates the five admin lifecycle calls. Maps each
/// API outcome to a concise, admin-facing message:
/// <list type="bullet">
///   <item><description><c>401</c> — surfaced as <see cref="ApiResult.ForceSignOut"/> so the controller redirects to login.</description></item>
///   <item><description><c>403</c> — "Not allowed" (matches the Phase 5B copy spec).</description></item>
///   <item><description><c>404</c> — "User not found."</description></item>
///   <item><description><c>409</c> — uses the API's specific Conflict message (lifecycle ineligibility, email already in use) since the backend already returns admin-readable text per the Phase 3A/B/C contracts.</description></item>
///   <item><description><c>400/422</c> — propagates the per-field validation map.</description></item>
///   <item><description>Other — generic "Action could not be completed." with API message if present.</description></item>
/// </list>
/// </summary>
public sealed class LifecycleFacade
{
    private readonly LifecycleApiClient _client;
    public LifecycleFacade(LifecycleApiClient client) => _client = client;

    public async Task<ApiResult> SuspendAsync(Guid userId, CancellationToken ct = default)
        => Map(await _client.SuspendAsync(userId, ct), "Could not suspend the user.");

    public async Task<ApiResult> ReactivateAsync(Guid userId, CancellationToken ct = default)
        => Map(await _client.ReactivateAsync(userId, ct), "Could not reactivate the user.");

    public async Task<ApiResult> ArchiveAsync(Guid userId, CancellationToken ct = default)
        => Map(await _client.ArchiveAsync(userId, ct), "Could not archive the user.");

    public async Task<ApiResult> ResetPasswordAsync(
        Guid userId, AdminResetPasswordVm vm, CancellationToken ct = default)
    {
        var result = await _client.ResetPasswordAsync(
            userId,
            new AdminResetPasswordRequest { Reason = vm.Reason?.Trim() },
            ct);
        return Map(result, "Could not initiate the password reset.");
    }

    public async Task<ApiResult> ReassignAsync(
        Guid userId, AdminReassignAccountVm vm, CancellationToken ct = default)
    {
        var result = await _client.ReassignAsync(
            userId,
            new AdminReassignAccountRequest
            {
                NewEmail = vm.NewEmail.Trim(),
                Reason   = vm.Reason?.Trim(),
            },
            ct);
        return Map(result, "Could not reassign the account.");
    }

    // §8.13 — force-revoke all of a user's active sessions and refresh tokens.
    public async Task<ApiResult> RevokeSessionsAsync(Guid userId, CancellationToken ct = default)
        => Map(await _client.RevokeSessionsAsync(userId, ct), "Could not revoke the user's sessions.");

    /// <summary>
    /// Phase 5B — single shared error mapper so every admin lifecycle
    /// verb surfaces consistent admin-facing copy. The backend
    /// produces user-readable Conflict messages (lifecycle ineligibility,
    /// email-in-use, etc.); we forward them verbatim. Validation
    /// payloads survive intact for ModelState.
    /// </summary>
    private static ApiResult Map(ApiResult result, string genericFallback)
    {
        if (result.IsSuccess)              return ApiResult.Ok();
        if (result.IsUnauthorized)         return ApiResult.ForceSignOut();
        if (result.IsForbidden)            return ApiResult.Fail(403, "Not allowed.");
        if (result.IsNotFound)             return ApiResult.Fail(404, "User not found.");
        if (result.IsConflict)             return ApiResult.Fail(409, result.Error ?? "This action is not allowed in the current state.");
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.Invalid(result.ValidationErrors);
        if (result.IsTooManyRequests)      return ApiResult.Fail(429, "Too many requests. Please try again later.");

        return ApiResult.Fail(result.StatusCode, result.Error ?? genericFallback);
    }
}
