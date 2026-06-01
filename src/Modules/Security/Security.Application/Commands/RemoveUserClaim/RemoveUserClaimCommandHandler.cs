using Microsoft.EntityFrameworkCore;
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
    public async Task<Result> Handle(RemoveUserClaimCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        // Hierarchy: user claims can grant effective permissions. Actor must
        // outrank the target user to mutate them.
        var guard = await hierarchy.EnsureCanManageUserAsync(request.UserId, cancellationToken);
        if (!guard.IsSuccess)
            return guard;

        var claim = await userClaimRepository.GetByIdAsync(request.ClaimId, cancellationToken);
        if (claim is null || claim.UserId != request.UserId)
        {
            return Result.Failure(UserClaimErrors.NotFound, Outcome.NotFound);
        }

        userClaimRepository.Remove(claim);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("UserClaim.ConcurrencyConflict", "User claim was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(request.UserId), cancellationToken);
        return Result.Success();
    }
}
