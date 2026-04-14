using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Mappers;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Requests;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users;

public sealed class UsersFacade
{
    private readonly UsersApiClient _users;
    private readonly RolesApiClient _roles;

    public UsersFacade(UsersApiClient users, RolesApiClient roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<ApiResult<UserListVm>> GetUsersAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _users.GetUsersAsync(page, pageSize, ct);

        if (result.IsSuccess)
        {
            var d  = result.Data!;
            var vm = new UserListVm
            {
                Users       = d.Items.Select(UsersMapper.ToRowVm).ToList(),
                Page        = d.PageNumber,
                PageSize    = d.PageSize,
                TotalCount  = d.TotalCount,
                HasPrevious = d.HasPreviousPage,
                HasNext     = d.HasNextPage,
            };
            return ApiResult<UserListVm>.CreateSuccess(vm);
        }

        if (result.IsUnauthorized) return ApiResult<UserListVm>.ForceSignOut();
        return ApiResult<UserListVm>.CreateSuccess(result.Error ?? "Could not load users.");
    }

    public async Task<ApiResult<UserDetailsVm>> GetDetailsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var userTask  = _users.GetUserAsync(userId, ct);
        var rolesTask = _roles.GetRolesAsync(ct);
        await Task.WhenAll(userTask, rolesTask);

        if (userTask.Result.IsUnauthorized || rolesTask.Result.IsUnauthorized)
            return ApiResult<UserDetailsVm>.ForceSignOut();

        if (!userTask.Result.IsSuccess)
        {
            return ApiResult<UserDetailsVm>.CreateFailure(
                userTask.Result.IsNotFound ? "User not found." : userTask.Result.Error ?? "Could not load user.");
        }

        var user  = userTask.Result.Data!;
        var roles = rolesTask.Result.Data ?? [];

        var vm = new UserDetailsVm
        {
            UserId       = user.Id,
            Email        = user.Email,
            IsActive     = user.IsActive,
            CurrentRoles = user.Roles,
            AvailableRoles = roles
                .Where(r => r.IsActive)
                .Select(r => new RoleOptionVm { Id = r.Id, Name = r.Name })
                .ToList(),
        };
        return ApiResult<UserDetailsVm>.CreateSuccess(vm);
    }

    public async Task<ApiResult> ActivateAsync(Guid userId, CancellationToken ct = default)
    {
        var result = await _users.ActivateUserAsync(userId, ct);
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        return ApiResult.Fail(result.Error ?? "Activate failed.");
    }

    public async Task<ApiResult> DeactivateAsync(Guid userId, CancellationToken ct = default)
    {
        var result = await _users.DeactivateUserAsync(userId, ct);
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        return ApiResult.Fail(result.Error ?? "Deactivate failed.");
    }

    public async Task<ApiResult> AssignRoleAsync(
        Guid userId, AssignRoleVm vm, CancellationToken ct = default)
    {
        var result = await _users.AssignRoleAsync(
            userId, UsersMapper.ToAssignRoleRequest(vm), ct);
        if (result.IsSuccess)   return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsConflict)  return ApiResult.Fail("User already has this role.");
        if (result.IsNotFound)  return ApiResult.Fail("User or role not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? "Could not assign role.");
    }

    public async Task<ApiResult> RemoveRoleAsync(
        Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var result = await _users.RemoveRoleAsync(userId, roleId, ct);
        if (result.IsSuccess || result.IsNotFound) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        return ApiResult.Fail(result.Error ?? "Could not remove role.");
    }

    public async Task<ApiResult> AddClaimAsync(
        Guid userId, AddClaimVm vm, CancellationToken ct = default)
    {
        var result = await _users.AddClaimAsync(
            userId, UsersMapper.ToAddClaimRequest(vm), ct);
        if (result.IsSuccess)   return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsConflict)  return ApiResult.Fail("Claim already exists.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? "Could not add claim.");
    }

    public async Task<ApiResult> RemoveClaimAsync(
        Guid userId, Guid claimId, CancellationToken ct = default)
    {
        var result = await _users.RemoveClaimAsync(userId, claimId, ct);
        if (result.IsSuccess || result.IsNotFound) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        return ApiResult.Fail(result.Error ?? "Could not remove claim.");
    }
}
