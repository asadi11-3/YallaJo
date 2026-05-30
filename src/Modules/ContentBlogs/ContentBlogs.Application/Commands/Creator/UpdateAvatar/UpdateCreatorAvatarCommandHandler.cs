using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.UpdateAvatar;

public sealed class UpdateCreatorAvatarCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateCreatorAvatarCommandHandler> logger)
    : ICommandHandler<UpdateCreatorAvatarCommand>
{
    public async Task<Result> Handle(
        UpdateCreatorAvatarCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileRepository
                .GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
                return Result.Failure(CreatorProfileErrors.NotFound, Outcome.NotFound);

            profile.UpdateAvatar(request.AvatarUrl);
            profileRepository.Update(profile);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileTag(profile.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileUserTag(profile.UserId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator avatar updated for profile {ProfileId}", profile.Id);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
