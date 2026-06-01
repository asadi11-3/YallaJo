using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.RemoveRole;

public sealed class RemoveRoleCommandHandler(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<RemoveRoleCommand>
{
    public async Task<Result> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        // Hierarchy: actor must strictly outrank the role being removed.
        // This replaces the previous "OwnerOnly for Owner/SuperAdmin" shortcut
        // with a uniform rule that also blocks Admin-removing-Admin.
        var roleGuard = hierarchy.EnsureCanManageRole(role.Name);
        if (!roleGuard.IsSuccess)
            return roleGuard;

        // Hierarchy: actor must also outrank the target user (prevents Admin
        // from removing roles from a SuperAdmin/Owner, and same-level edits).
        var targetGuard = await hierarchy.EnsureCanManageUserAsync(request.UserId, cancellationToken);
        if (!targetGuard.IsSuccess)
            return targetGuard;

        var userRole = await userRepository.GetUserRoleAsync(request.UserId, request.RoleId, cancellationToken);
        if (userRole is null)
            return Result.Failure(RoleErrors.NotAssigned, Outcome.NotFound);

        userRepository.RemoveUserRole(userRole);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("UserRole.ConcurrencyConflict", "Role assignment was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(request.UserId), cancellationToken);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result.Success();
    }
}
