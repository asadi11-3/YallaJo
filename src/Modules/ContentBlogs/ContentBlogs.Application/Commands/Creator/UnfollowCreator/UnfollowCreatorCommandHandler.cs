using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.UnfollowCreator;

public sealed class UnfollowCreatorCommandHandler(
    ICreatorProfileRepository profileRepository,
    ICreatorFollowRepository followRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UnfollowCreatorCommandHandler> logger)
    : ICommandHandler<UnfollowCreatorCommand>
{
    public async Task<Result> Handle(
        UnfollowCreatorCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        var userId = currentUser.UserId!.Value;

            var follow = await followRepository
                .GetAsync(userId, request.CreatorProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (follow is null)
            {
                return Result.Failure(
                    CreatorProfileErrors.NotFollowing, Outcome.NotFound);
            }

            followRepository.Remove(follow);

            // Raise domain event from the aggregate root so UoW dispatches it.
            var profile = await profileRepository
                .GetByIdAsync(request.CreatorProfileId, cancellationToken)
                .ConfigureAwait(false);

            profile?.RecordUnfollow(userId);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Creator.ConcurrencyConflict",
                        "Follow was modified concurrently."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorFollowersTag(request.CreatorProfileId), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileTag(request.CreatorProfileId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator unfollowed: ProfileId={ProfileId}, FollowerUserId={UserId}",
                request.CreatorProfileId, userId);

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
