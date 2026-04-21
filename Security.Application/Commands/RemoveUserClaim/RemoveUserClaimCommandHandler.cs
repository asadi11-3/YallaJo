using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.RemoveUserClaim;

public sealed class RemoveUserClaimCommandHandler(
    IUserRepository userRepository,
    IUserClaimRepository userClaimRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<RemoveUserClaimCommand>
{
    public async Task<Result> Handle(RemoveUserClaimCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        // Hierarchy: user claims can grant effective permissions. Actor must
        // outrank the target user to mutate them.
        var guard = await hierarchy.EnsureCanManageUserAsync(request.UserId, ct);
        if (!guard.IsSuccess)
            return guard;

        var claim = await userClaimRepository.GetByIdAsync(request.ClaimId, ct);
        if (claim is null || claim.UserId != request.UserId)
        {
            return Result.Failure(
               new Error("NotFound.UserClaim", "The specified claim was not found on this user."),
               Outcome.NotFound);
        }

        userClaimRepository.Remove(claim);
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(request.UserId), ct);
        return Result.Success();
    }
}

