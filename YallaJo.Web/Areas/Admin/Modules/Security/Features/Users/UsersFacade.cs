using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Mappers;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Requests;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.ViewModels;

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

    // ── List ──────────────────────────────────────────────────────────────────

    public async Task<UsersFacadeResult<UserListVm>> GetUsersAsync(
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
            return UsersFacadeResult<UserListVm>.Ok(vm);
        }

        if (result.IsUnauthorized) return UsersFacadeResult<UserListVm>.ForceSignOut();
        return UsersFacadeResult<UserListVm>.Fail(result.Error ?? "Could not load users.");
    }

    // ── Details ───────────────────────────────────────────────────────────────

    public async Task<UsersFacadeResult<UserDetailsVm>> GetDetailsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var userTask  = _users.GetUserAsync(userId, ct);
        var rolesTask = _roles.GetRolesAsync(ct);
        await Task.WhenAll(userTask, rolesTask);

        if (userTask.Result.IsUnauthorized || rolesTask.Result.IsUnauthorized)
            return UsersFacadeResult<UserDetailsVm>.ForceSignOut();

        if (!userTask.Result.IsSuccess)
            return UsersFacadeResult<UserDetailsVm>.Fail(
                userTask.Result.IsNotFound ? "User not found." : userTask.Result.Error ?? "Could not load user.");

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
        return UsersFacadeResult<UserDetailsVm>.Ok(vm);
    }

    // ── Activate / Deactivate ─────────────────────────────────────────────────

    public async Task<UsersFacadeResult> ActivateAsync(Guid userId, CancellationToken ct = default)
    {
        var result = await _users.ActivateUserAsync(userId, ct);
        if (result.IsSuccess) return UsersFacadeResult.Ok();
        if (result.IsUnauthorized) return UsersFacadeResult.ForceSignOut();
        return UsersFacadeResult.Fail(result.Error ?? "Activate failed.");
    }

    public async Task<UsersFacadeResult> DeactivateAsync(Guid userId, CancellationToken ct = default)
    {
        var result = await _users.DeactivateUserAsync(userId, ct);
        if (result.IsSuccess) return UsersFacadeResult.Ok();
        if (result.IsUnauthorized) return UsersFacadeResult.ForceSignOut();
        return UsersFacadeResult.Fail(result.Error ?? "Deactivate failed.");
    }

    // ── Role assignment ───────────────────────────────────────────────────────

    public async Task<UsersFacadeResult> AssignRoleAsync(
        Guid userId, AssignRoleVm vm, CancellationToken ct = default)
    {
        var result = await _users.AssignRoleAsync(
            userId, UsersMapper.ToAssignRoleRequest(vm), ct);
        if (result.IsSuccess)   return UsersFacadeResult.Ok();
        if (result.IsUnauthorized) return UsersFacadeResult.ForceSignOut();
        if (result.IsConflict)  return UsersFacadeResult.Fail("User already has this role.");
        if (result.IsNotFound)  return UsersFacadeResult.Fail("User or role not found.");
        if (result.IsValidationError) return UsersFacadeResult.Invalid(result.ValidationErrors!);
        return UsersFacadeResult.Fail(result.Error ?? "Could not assign role.");
    }

    public async Task<UsersFacadeResult> RemoveRoleAsync(
        Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var result = await _users.RemoveRoleAsync(userId, roleId, ct);
        if (result.IsSuccess || result.IsNotFound) return UsersFacadeResult.Ok();
        if (result.IsUnauthorized) return UsersFacadeResult.ForceSignOut();
        return UsersFacadeResult.Fail(result.Error ?? "Could not remove role.");
    }

    // ── Claims ────────────────────────────────────────────────────────────────

    public async Task<UsersFacadeResult> AddClaimAsync(
        Guid userId, AddClaimVm vm, CancellationToken ct = default)
    {
        var result = await _users.AddClaimAsync(
            userId, UsersMapper.ToAddClaimRequest(vm), ct);
        if (result.IsSuccess)   return UsersFacadeResult.Ok();
        if (result.IsUnauthorized) return UsersFacadeResult.ForceSignOut();
        if (result.IsConflict)  return UsersFacadeResult.Fail("Claim already exists.");
        if (result.IsValidationError) return UsersFacadeResult.Invalid(result.ValidationErrors!);
        return UsersFacadeResult.Fail(result.Error ?? "Could not add claim.");
    }

    public async Task<UsersFacadeResult> RemoveClaimAsync(
        Guid userId, Guid claimId, CancellationToken ct = default)
    {
        var result = await _users.RemoveClaimAsync(userId, claimId, ct);
        if (result.IsSuccess || result.IsNotFound) return UsersFacadeResult.Ok();
        if (result.IsUnauthorized) return UsersFacadeResult.ForceSignOut();
        return UsersFacadeResult.Fail(result.Error ?? "Could not remove claim.");
    }
}

// ── Facade result types ────────────────────────────────────────────────────────

public sealed class UsersFacadeResult
{
    public bool    IsSuccess      { get; private init; }
    public string? Error          { get; private init; }
    public bool    RequireSignOut { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public static UsersFacadeResult Ok()           => new() { IsSuccess = true };
    public static UsersFacadeResult Fail(string e) => new() { IsSuccess = false, Error = e };
    public static UsersFacadeResult ForceSignOut() => new() { IsSuccess = false, RequireSignOut = true };
    public static UsersFacadeResult Invalid(IReadOnlyDictionary<string, string[]> errs)
        => new() { IsSuccess = false, ValidationErrors = errs };
}

public sealed class UsersFacadeResult<T>
{
    public bool    IsSuccess      { get; private init; }
    public T?      Data           { get; private init; }
    public string? Error          { get; private init; }
    public bool    RequireSignOut { get; private init; }

    public static UsersFacadeResult<T> Ok(T data)    => new() { IsSuccess = true, Data = data };
    public static UsersFacadeResult<T> Fail(string e)=> new() { IsSuccess = false, Error = e };
    public static UsersFacadeResult<T> ForceSignOut() => new() { IsSuccess = false, RequireSignOut = true };
}
