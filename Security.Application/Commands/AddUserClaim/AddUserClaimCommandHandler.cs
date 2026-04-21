using Microsoft.Extensions.Caching.Hybrid;
using Security.Application.Authorization;
using Security.Application.Caching;
using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.AddUserClaim;

public sealed class AddUserClaimCommandHandler(
    IUserRepository userRepository,
    IUserClaimRepository userClaimRepository,
    ISecurityUnitOfWork unitOfWork,
    IRoleHierarchyService hierarchy,
    HybridCache cache)
    : ICommandHandler<AddUserClaimCommand>
{
    public async Task<Result> Handle(AddUserClaimCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user is null)
            return Result.Failure(UserErrors.NotFound, Outcome.NotFound);

        // Hierarchy: direct user claims can silently grant effective permissions
        // (a privilege-escalation vector). Require actor to outrank the target user.
        var guard = await hierarchy.EnsureCanManageUserAsync(request.UserId, ct);
        if (!guard.IsSuccess)
            return guard;

        var alreadyExists = await userClaimRepository.AnyAsync(
            uc => uc.UserId == request.UserId
               && uc.ClaimType == request.ClaimType
               && uc.ClaimValue == request.ClaimValue,
            ct);

        if (alreadyExists)
        {
            return Result.Failure(
             new Error("UserClaim.Duplicate", "This claim already exists on the user."),
             Outcome.Conflict);
        }

        var claim = UserClaim.Create(request.UserId, request.ClaimType, request.ClaimValue);
        await userClaimRepository.AddAsync(claim, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // UserDto does not expose claims directly, but invalidate per-user cache
        // to ensure any future extension remains consistent.
        await cache.RemoveByTagAsync(SecurityCacheKeys.UserTag(request.UserId), ct);

        return Result.Success();
    }
}

