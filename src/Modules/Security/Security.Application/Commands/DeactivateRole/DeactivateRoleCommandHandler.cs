using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.DeactivateRole;

public sealed class DeactivateRoleCommandHandler(
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<DeactivateRoleCommand>
{
    public async Task<Result> Handle(DeactivateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken, asNoTracking: false);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        // Hierarchy: deactivating a role silently strips permissions from
        // every holder. Actor must strictly outrank the role.
        var roleGuard = hierarchy.EnsureCanModifyRoleDefinition(role.Name);
        if (!roleGuard.IsSuccess)
            return roleGuard;

        if (!role.IsActive)
            return Result.Success(); // idempotent — already deactivated

        role.Deactivate();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("Role.ConcurrencyConflict", "Role was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        // Invalidate roles list AND all user caches: GetUserQuery filters by ur.Role.IsActive,
        // so deactivating a role changes which roles appear in cached UserDto.Roles.
        await cache.RemoveByTagAsync(SecurityCacheKeys.RolesTag, cancellationToken);
        await cache.RemoveByTagAsync(SecurityCacheKeys.UsersTag, cancellationToken);

        return Result.Success();
    }
}
