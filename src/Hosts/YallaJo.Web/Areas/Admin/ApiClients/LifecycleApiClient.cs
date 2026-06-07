using YallaJo.Web.Areas.Admin.Models.Lifecycle;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

using YallaJo.Web.Areas.Admin.Models.Users;
namespace YallaJo.Web.Areas.Admin.ApiClients;

/// <summary>
/// Phase 5B — typed wrapper around the Phase 3A/3B/3C admin lifecycle
/// endpoints on <c>/api/v1/auth/admin/users/{userId}/...</c>.
/// <para>
/// Each method returns the underlying <see cref="ApiResult"/> so the
/// facade can inspect <c>IsForbidden</c> / <c>IsConflict</c> /
/// validation errors and surface a typed message to the controller.
/// All methods are pure HTTP — no swallowing, no logging.
/// </para>
/// </summary>
public sealed class LifecycleApiClient
{
    private readonly IApiClient _api;
    public LifecycleApiClient(IApiClient api) => _api = api;

    // ── Phase 3B: lifecycle transitions ───────────────────────────────────────

    public Task<ApiResult> SuspendAsync(Guid userId, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/auth/admin/users/{userId}/suspend", null, ct);

    public Task<ApiResult> ReactivateAsync(Guid userId, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/auth/admin/users/{userId}/reactivate", null, ct);

    public Task<ApiResult> ArchiveAsync(Guid userId, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/auth/admin/users/{userId}/archive", null, ct);

    // ── Phase 3A: admin-initiated password reset ──────────────────────────────

    public Task<ApiResult> ResetPasswordAsync(
        Guid userId,
        AdminResetPasswordRequest request,
        CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/auth/admin/users/{userId}/reset-password", request, ct);

    // ── Phase 3C: admin reassignment ──────────────────────────────────────────

    public Task<ApiResult> ReassignAsync(
        Guid userId,
        AdminReassignAccountRequest request,
        CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/auth/admin/users/{userId}/reassign", request, ct);

    // ── §8.13: force-revoke all sessions for a user ───────────────────────────
    // DELETE /api/v1/auth/admin/users/{userId}/sessions
    public Task<ApiResult> RevokeSessionsAsync(Guid userId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/auth/admin/users/{userId}/sessions", ct);
}
