using YallaJo.Web.Areas.Admin.Models.Roles;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class RolesApiClient
{
    private readonly IApiClient _api;
    public RolesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<RoleItemResponse>>> GetRolesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<RoleItemResponse>>("/api/v1/security/roles", ct);

    public Task<ApiResult<RoleDetailsResponse>> GetRoleAsync(Guid roleId, CancellationToken ct = default)
        => _api.GetAsync<RoleDetailsResponse>($"/api/v1/security/roles/{roleId}", ct);

    public Task<ApiResult<CreateRoleResponse>> CreateRoleAsync(
        CreateRoleRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateRoleResponse>("/api/v1/security/roles", request, ct);

    public Task<ApiResult> UpdateRoleAsync(
        Guid roleId, UpdateRoleRequest request, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/security/roles/{roleId}", request, ct);

    public Task<ApiResult> DeactivateRoleAsync(Guid roleId, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/security/roles/{roleId}/deactivate", null, ct);

    public Task<ApiResult> AddClaimAsync(
        Guid roleId, AddRoleClaimRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/security/roles/{roleId}/claims", request, ct);

    public Task<ApiResult> RemoveClaimAsync(
        Guid roleId, Guid claimId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/security/roles/{roleId}/claims/{claimId}", ct);
}
