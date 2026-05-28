using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.FollowCreator;

public sealed class FollowCreatorCommandHandler(
    ICreatorProfileRepository profileRepository,
    ICreatorFollowRepository followRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<FollowCreatorCommandHandler> logger)
    : ICommandHandler<FollowCreatorCommand>
{
    public async Task<Result> Handle(
        FollowCreatorCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        var userId = currentUser.UserId!.Value;

            if (userId == request.CreatorProfileId)
            {
                return Result.Failure(
                    CreatorProfileErrors.CannotFollowSelf, Outcome.UnprocessableEntity);
            }

            var profile = await profileRepository
                .GetByIdAsync(request.CreatorProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return Result.Failure(
                    CreatorProfileErrors.NotFound, Outcome.NotFound);
            }

            if (profile.UserId == userId)
            {
                return Result.Failure(
                    CreatorProfileErrors.CannotFollowSelf, Outcome.UnprocessableEntity);
            }

            if (await followRepository
                .ExistsAsync(userId, request.CreatorProfileId, cancellationToken)
                .ConfigureAwait(false))
            {
                return Result.Failure(
                    CreatorProfileErrors.AlreadyFollowing, Outcome.Conflict);
            }

            var followResult = CreatorFollow.Create(userId, request.CreatorProfileId);
            if (followResult.IsFailure)
            {
                return Result.Failure(
                    followResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
            }

            await followRepository
                .AddAsync(followResult.Value, cancellationToken)
                .ConfigureAwait(false);

            // Raise domain event from the aggregate root so UoW dispatches it.
            // CreatorFollow is a BaseEntity (not IAggregateRoot), so events must
            // originate from CreatorProfile for the outbox to stage them.
            profile.RecordFollow(userId);

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
                "Creator followed: ProfileId={ProfileId}, FollowerUserId={UserId}",
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
