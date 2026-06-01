using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.UpdateRole;

public sealed class UpdateRoleCommandHandler(
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<UpdateRoleCommand>
{
    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken, asNoTracking: false);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        // Hierarchy: only actors strictly outranking the role may edit it.
        // This covers the previous "Protected for Owner/SuperAdmin" rule and
        // additionally blocks Admin-editing-Admin.
        var roleGuard = hierarchy.EnsureCanModifyRoleDefinition(role.Name);
        if (!roleGuard.IsSuccess)
            return roleGuard;

        role.UpdateDescription(request.Description);

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

        await cache.RemoveByTagAsync(SecurityCacheKeys.RolesTag, cancellationToken);

        return Result.Success();
    }
}
