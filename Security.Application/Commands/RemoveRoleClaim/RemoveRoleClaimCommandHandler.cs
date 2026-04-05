using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Caching;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.RemoveRoleClaim;

public sealed class RemoveRoleClaimCommandHandler(
    IRoleRepository roleRepository,
    IRoleClaimRepository roleClaimRepository,
    ISecurityUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<RemoveRoleClaimCommand>
{
    public async Task<Result> Handle(RemoveRoleClaimCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var role = await roleRepository.GetByIdAsync(request.RoleId, ct);
        if (role is null)
            return Result.Failure(RoleErrors.NotFound, Outcome.NotFound);

        var claim = await roleClaimRepository.GetByIdAsync(request.ClaimId, ct);
        if (claim is null || claim.RoleId != request.RoleId)
        {
            return Result.Failure(
               new Error("NotFound.RoleClaim", "The specified claim was not found on this role."),
               Outcome.NotFound);
        }
           

        roleClaimRepository.Remove(claim);
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(SecurityCacheKeys.RolesTag, ct);

        return Result.Success();
    }
}
