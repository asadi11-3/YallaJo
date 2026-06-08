using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.ActivateUser;

public sealed class ActivateUserCommandHandler(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<ActivateUserCommand>
{
    public async Task<Result> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken, asNoTracking: false);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        // Hierarchy: activation can silently restore elevated privileges —
        // require actor to outrank the target user.
        var guard = await hierarchy.EnsureCanManageUserAsync(request.UserId, cancellationToken);
        if (!guard.IsSuccess)
            return guard;

        if (user.IsActive)
            return Result.Success(); // idempotent — already active

        // Errors-as-values: an archived (terminal) account cannot be reactivated.
        // The domain enforces this invariant by throwing; translate to a Conflict result
        // instead of letting the exception escape.
        try
        {
            user.Activate();
        }
        catch (InvalidLifecycleTransitionException ex)
        {
            return Result.Failure(
                new Error("User.InvalidLifecycleTransition", ex.Message),
                Outcome.Conflict);
        }

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
