using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.DeactivateUser;

public sealed class DeactivateUserCommandHandler(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<DeactivateUserCommand>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken, asNoTracking: false);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        // Hierarchy: deactivation is an effective privilege change — require
        // actor to outrank the target user (prevents Admin locking SuperAdmin).
        var guard = await hierarchy.EnsureCanManageUserAsync(request.UserId, cancellationToken);
        if (!guard.IsSuccess)
            return guard;

        if (!user.IsActive)
            return Result.Success(); // idempotent — already deactivated

        user.Deactivate();

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
