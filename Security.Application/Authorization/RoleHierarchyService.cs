using Security.Contracts.Authorization;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Authorization;


public sealed class RoleHierarchyService(
    ICurrentUser currentUser,
    IUserRepository userRepository)
    : IRoleHierarchyService
{
    public RolePrivilegeLevel GetActingUserLevel()
    {
        if (!currentUser.IsAuthenticated)
        {
            return RolePrivilegeLevel.None;
        }

        return AppRoles.HighestPrivilegeLevel(currentUser.Roles);
    }

    public async Task<RolePrivilegeLevel> GetTargetUserLevelAsync(
        Guid targetUserId,
        CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdWithDetailsAsync(targetUserId, ct);
        if (user is null)
        {
            return RolePrivilegeLevel.None;
        }

        var roleNames = user.UserRoles
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role.Name);

        return AppRoles.HighestPrivilegeLevel(roleNames);
    }

    public async Task<Result> EnsureCanManageUserAsync(
        Guid targetUserId,
        CancellationToken ct = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure(UserErrors.Unauthorized, Outcome.Unauthorized);
        }

        var actingLevel = GetActingUserLevel();
        if (actingLevel <= RolePrivilegeLevel.Standard)
        {
            // Non-privileged actors must never reach admin-management flows.
            return Result.Failure(UserErrors.InsufficientPrivilege, Outcome.Forbidden);
        }

        // Self-management of privileged level is not allowed through admin paths.
        if (currentUser.UserId == targetUserId)
        {
            return Result.Failure(UserErrors.CannotManageSelfPrivilege, Outcome.Forbidden);
        }

        var targetLevel = await GetTargetUserLevelAsync(targetUserId, ct);

        if (actingLevel == targetLevel)
        {
            return Result.Failure(UserErrors.SameLevelForbidden, Outcome.Forbidden);
        }

        if (actingLevel < targetLevel)
        {
            return Result.Failure(UserErrors.InsufficientPrivilege, Outcome.Forbidden);
        }

        return Result.Success();
    }

    public Result EnsureCanManageRole(string roleName)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure(UserErrors.Unauthorized, Outcome.Unauthorized);
        }

        var actingLevel = GetActingUserLevel();
        var roleLevel = AppRoles.GetPrivilegeLevel(roleName);

        // Actor must strictly outrank the role. Assigning a Standard role needs
        // at least Admin; assigning Admin needs SuperAdmin; assigning SuperAdmin
        // needs Owner. Standard actors cannot assign any privileged role.
        if (actingLevel <= RolePrivilegeLevel.Standard)
        {
            return Result.Failure(UserErrors.InsufficientPrivilege, Outcome.Forbidden);
        }

        if (actingLevel <= roleLevel)
        {
            return Result.Failure(UserErrors.RoleBelowActor, Outcome.Forbidden);
        }

        return Result.Success();
    }

    public Result EnsureCanModifyRoleDefinition(string roleName)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure(UserErrors.Unauthorized, Outcome.Unauthorized);
        }

        var actingLevel = GetActingUserLevel();
        var roleLevel = AppRoles.GetPrivilegeLevel(roleName);

        if (actingLevel <= RolePrivilegeLevel.Standard)
        {
            return Result.Failure(UserErrors.InsufficientPrivilege, Outcome.Forbidden);
        }

        if (actingLevel <= roleLevel)
        {
            return Result.Failure(UserErrors.RoleBelowActor, Outcome.Forbidden);
        }

        return Result.Success();
    }
}
