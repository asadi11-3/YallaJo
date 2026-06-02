using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.RemoveRoleClaim;

public sealed class RemoveRoleClaimCommandHandler(
    IRoleRepository roleRepository,
    IRoleClaimRepository roleClaimRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<RemoveRoleClaimCommand>
{
    public async Task<Result> Handle(RemoveRoleClaimCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        // Hierarchy: removing a claim from a role silently revokes permissions
        // from every holder. Actor must strictly outrank the role.
        var roleGuard = hierarchy.EnsureCanModifyRoleDefinition(role.Name);
        if (!roleGuard.IsSuccess)
            return roleGuard;

        var claim = await roleClaimRepository.GetByIdAsync(request.ClaimId, cancellationToken);
        if (claim is null || claim.RoleId != request.RoleId)
        {
            return Result.Failure(RoleClaimErrors.NotFound, Outcome.NotFound);
        }

        roleClaimRepository.Remove(claim);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("RoleClaim.ConcurrencyConflict", "Role claim was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(SecurityCacheKeys.RolesTag, cancellationToken);

        return Result.Success();
    }
}
