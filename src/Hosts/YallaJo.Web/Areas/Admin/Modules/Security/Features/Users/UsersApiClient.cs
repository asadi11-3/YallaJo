using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Requests;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users;

public sealed class UsersApiClient
{
    private readonly IApiClient _api;
    public UsersApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<UserListResponse>> GetUsersAsync(
        int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<UserListResponse>(
            $"/api/v1/security/users?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<UserItemResponse>> GetUserAsync(
        Guid userId, CancellationToken ct = default)
        => _api.GetAsync<UserItemResponse>(
            $"/api/v1/security/users/{userId}", ct);

    public Task<ApiResult> ActivateUserAsync(Guid userId, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/security/users/{userId}/activate", null, ct);

    public Task<ApiResult> DeactivateUserAsync(Guid userId, CancellationToken ct = default)
        => _api.PatchAsync($"/api/v1/security/users/{userId}/deactivate", null, ct);

    public Task<ApiResult> AssignRoleAsync(
        Guid userId, AssignRoleRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/security/users/{userId}/roles", request, ct);

    public Task<ApiResult> RemoveRoleAsync(
        Guid userId, Guid roleId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/security/users/{userId}/roles/{roleId}", ct);

    public Task<ApiResult> AddClaimAsync(
        Guid userId, AddUserClaimRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/security/users/{userId}/claims", request, ct);

    public Task<ApiResult> RemoveClaimAsync(
        Guid userId, Guid claimId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/security/users/{userId}/claims/{claimId}", ct);
}
