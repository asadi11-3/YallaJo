using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Mappers;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles;

public sealed class RolesFacade
{
    private readonly RolesApiClient _api;
    public RolesFacade(RolesApiClient api) => _api = api;

    public async Task<ApiResult<RoleListVm>> GetRolesAsync(CancellationToken ct = default)
    {
        var raw = await _api.GetRolesAsync(ct);
        if (!raw.IsSuccess)
            return ApiResult<RoleListVm>.CreateFailure(raw.StatusCode, raw.Error);

        var vm = new RoleListVm
        {
            Roles = (raw.Data ?? []).Select(RolesMapper.ToRowVm).ToList(),
        };
        return ApiResult<RoleListVm>.CreateSuccess(vm);
    }

    public async Task<ApiResult<RoleDetailsVm>> GetDetailsAsync(Guid roleId, CancellationToken ct = default)
    {
        var raw = await _api.GetRoleAsync(roleId, ct);
        if (!raw.IsSuccess || raw.Data is null)
            return ApiResult<RoleDetailsVm>.CreateFailure(raw.StatusCode, raw.Error);

        return ApiResult<RoleDetailsVm>.CreateSuccess(RolesMapper.ToDetailsVm(raw.Data));
    }

    public async Task<ApiResult> CreateAsync(CreateRoleVm vm, CancellationToken ct = default)
        => ToVoidResult(await _api.CreateRoleAsync(RolesMapper.ToCreateRequest(vm), ct));
    public Task<ApiResult> UpdateAsync(Guid roleId, UpdateRoleVm vm, CancellationToken ct = default)
        => _api.UpdateRoleAsync(roleId, RolesMapper.ToUpdateRequest(vm), ct);

    public Task<ApiResult> DeactivateAsync(Guid roleId, CancellationToken ct = default)
        => _api.DeactivateRoleAsync(roleId, ct);


    public Task<ApiResult> AddClaimAsync(Guid roleId, AddRoleClaimVm vm, CancellationToken ct = default)
        => _api.AddClaimAsync(roleId, RolesMapper.ToAddClaimRequest(vm), ct);

    public Task<ApiResult> RemoveClaimAsync(Guid roleId, Guid claimId, CancellationToken ct = default)
        => _api.RemoveClaimAsync(roleId, claimId, ct);
    private static ApiResult ToVoidResult<T>(ApiResult<T> r) =>
        r.IsSuccess
            ? ApiResult.Ok(r.StatusCode)
            : r.IsValidationError
                ? ApiResult.ValidationFail(r.StatusCode, r.ValidationErrors!)
                : ApiResult.Fail(r.StatusCode, r.Error);
}
