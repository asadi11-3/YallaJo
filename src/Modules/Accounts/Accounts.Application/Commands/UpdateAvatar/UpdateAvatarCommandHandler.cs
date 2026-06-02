using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.UpdateAvatar;

public sealed class UpdateAvatarCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<UpdateAvatarCommand, UpdateAvatarResult>
{
    public async Task<Result<UpdateAvatarResult>> Handle(
        UpdateAvatarCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<UpdateAvatarResult>.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);
        }

        var userId = currentUser.UserId.Value;

        var profile = await profileRepository.FirstOrDefaultAsync(
            filter: p => p.UserId == userId,
            asNoTracking: false,
            ct: cancellationToken);

        if (profile is null)
        {
            return Result<UpdateAvatarResult>.Failure(
                ProfileErrors.NotFound,
                Outcome.NotFound);
        }

        profile.UpdateAvatar(request.AvatarUrl);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<UpdateAvatarResult>.Failure(
                new Error("Profile.ConcurrencyConflict", "The profile was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(AccountsCacheKeys.UserProfileTag(userId), cancellationToken);

        return Result<UpdateAvatarResult>.Success(new UpdateAvatarResult(profile.AvatarUrl!));
    }
}
