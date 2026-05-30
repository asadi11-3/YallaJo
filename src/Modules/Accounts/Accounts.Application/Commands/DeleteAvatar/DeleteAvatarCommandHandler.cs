using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.DeleteAvatar;

public sealed class DeleteAvatarCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<DeleteAvatarCommand, DeleteAvatarResult>
{
    public async Task<Result<DeleteAvatarResult>> Handle(
        DeleteAvatarCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<DeleteAvatarResult>.Failure(
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
            return Result<DeleteAvatarResult>.Failure(
                ProfileErrors.NotFound,
                Outcome.NotFound);
        }

        profile.DeleteAvatar();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(AccountsCacheKeys.UserProfileTag(userId), cancellationToken);

        return Result<DeleteAvatarResult>.Success(new DeleteAvatarResult(true));
    }
}
