using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Mappers;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles;

/// <summary>
/// Orchestrates all Roles admin operations.
/// Returns <see cref="ApiResult"/> / <see cref="ApiResult{T}"/> directly.
/// The caller (controller) branches on result.State — no custom facade result types.
/// </summary>
public sealed class RolesFacade
{
    private readonly RolesApiClient _api;
    public RolesFacade(RolesApiClient api) => _api = api;

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<ApiResult<RoleListVm>> GetRolesAsync(CancellationToken ct = default)
    {
        var raw = await _api.GetRolesAsync(ct);
        if (!raw.IsSuccess)
            return ApiResult<RoleListVm>.Fail(raw.StatusCode, raw.Error);

        var vm = new RoleListVm
        {
            Roles = (raw.Data ?? []).Select(RolesMapper.ToRowVm).ToList(),
        };
        return ApiResult<RoleListVm>.Ok(vm);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public Task<ApiResult> CreateAsync(CreateRoleVm vm, CancellationToken ct = default)
        => _api.CreateRoleAsync(RolesMapper.ToCreateRequest(vm), ct)
               .ContinueWith(t => ToVoidResult(t.Result), ct,
                   TaskContinuationOptions.ExecuteSynchronously,
                   TaskScheduler.Default);

    // ── Update description ────────────────────────────────────────────────────

    public Task<ApiResult> UpdateAsync(Guid roleId, UpdateRoleVm vm, CancellationToken ct = default)
        => _api.UpdateRoleAsync(roleId, RolesMapper.ToUpdateRequest(vm), ct);

    // ── Deactivate ────────────────────────────────────────────────────────────

    public Task<ApiResult> DeactivateAsync(Guid roleId, CancellationToken ct = default)
        => _api.DeactivateRoleAsync(roleId, ct);

    // ── Claims ────────────────────────────────────────────────────────────────

    public Task<ApiResult> AddClaimAsync(Guid roleId, AddRoleClaimVm vm, CancellationToken ct = default)
        => _api.AddClaimAsync(roleId, RolesMapper.ToAddClaimRequest(vm), ct);

    public Task<ApiResult> RemoveClaimAsync(Guid roleId, Guid claimId, CancellationToken ct = default)
        => _api.RemoveClaimAsync(roleId, claimId, ct);

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a typed ApiResult&lt;T&gt; (from CreateRole which returns a body) to a
    /// non-generic ApiResult — the controller only needs to know success/failure, not the data.
    /// </summary>
    private static ApiResult ToVoidResult<T>(ApiResult<T> r) =>
        r.IsSuccess
            ? ApiResult.Ok(r.StatusCode)
            : r.IsValidationError
                ? ApiResult.ValidationFail(r.StatusCode, r.ValidationErrors!)
                : ApiResult.Fail(r.StatusCode, r.Error);
}
