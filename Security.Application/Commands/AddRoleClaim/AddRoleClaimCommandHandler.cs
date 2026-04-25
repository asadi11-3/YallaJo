using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.AddRoleClaim;

public sealed class AddRoleClaimCommandHandler(
    IRoleRepository roleRepository,
    IRoleClaimRepository roleClaimRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<AddRoleClaimCommand>
{
    public async Task<Result> Handle(AddRoleClaimCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        // Hierarchy: adding a claim to a role effectively grants that permission
        // to every user who has the role. Actor must strictly outrank the role.
        var roleGuard = hierarchy.EnsureCanModifyRoleDefinition(role.Name);
        if (!roleGuard.IsSuccess)
            return roleGuard;

        var alreadyExists = await roleClaimRepository.AnyAsync(
            rc => rc.RoleId == request.RoleId
               && rc.ClaimType == request.ClaimType
               && rc.ClaimValue == request.ClaimValue,
            cancellationToken);

        if (alreadyExists)
        {
            return Result.Failure(
               new Error("RoleClaim.Duplicate", "This claim already exists on the role."),
               Outcome.Conflict);
        }

        var claim = RoleClaim.Create(request.RoleId, request.ClaimType, request.ClaimValue);
        await roleClaimRepository.AddAsync(claim, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(SecurityCacheKeys.RolesTag, cancellationToken);

        return Result.Success();
    }
}
