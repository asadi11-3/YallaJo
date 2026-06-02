using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Contracts.Authorization;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.AssignRole;

public sealed class AssignRoleCommandHandler(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<AssignRoleCommand>
{
    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        if (!role.IsActive)
            return Result.Failure(RoleErrors.Inactive, Outcome.Invalid);

        // Hierarchy: the actor must strictly outrank the role being assigned
        // (Admin cannot assign Admin; SuperAdmin cannot assign SuperAdmin; etc).
        // This supersedes the legacy OwnerOnly check while keeping its intent:
        // only Owner can assign Owner/SuperAdmin.
        var roleGuard = hierarchy.EnsureCanManageRole(role.Name);
        if (!roleGuard.IsSuccess)
            return roleGuard;

        // OwnerSingleton: only one user may hold the Owner role.
        if (role.Name == AppRoles.Owner
            && await userRepository.AnyWithRoleAsync(AppRoles.Owner, cancellationToken))
            return Result.Failure(RoleErrors.OwnerSingleton, Outcome.Conflict);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken, asNoTracking: false);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        // Hierarchy: actor must also outrank the target user. This prevents
        // Admin from touching SuperAdmin/Owner, and blocks same-level edits.
        var targetGuard = await hierarchy.EnsureCanManageUserAsync(request.UserId, cancellationToken);
        if (!targetGuard.IsSuccess)
            return targetGuard;

        var existing = await userRepository.GetUserRoleAsync(request.UserId, request.RoleId, cancellationToken);
        if (existing is not null)
            return Result.Failure(RoleErrors.AlreadyAssigned, Outcome.Conflict);

        user.AssignRole(role);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("User.ConcurrencyConflict", "User was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(request.UserId), cancellationToken);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result.Success();
    }
}
