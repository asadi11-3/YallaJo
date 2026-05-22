using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.Posts.DeletePost;

public sealed class DeleteCreatorPostCommandHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<DeleteCreatorPostCommandHandler> logger)
    : ICommandHandler<DeleteCreatorPostCommand>
{
    public async Task<Result> Handle(
        DeleteCreatorPostCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure(
                    new Error("Auth.UserIdMissing", "Authenticated user id is missing."),
                    Outcome.Unauthorized);
            }

            var profile = await profileRepository
                .GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return Result.Failure(
                    new Error("Creator.ProfileNotFound", "You do not have a creator profile."),
                    Outcome.NotFound);
            }

            var post = await postRepository
                .GetByIdAsync(request.PostId, cancellationToken)
                .ConfigureAwait(false);

            if (post is null || post.CreatorProfileId != profile.Id)
            {
                return Result.Failure(
                    CreatorPostErrors.NotFound,
                    Outcome.NotFound);
            }

            var canDeleteResult = post.CanBeDeletedByCreator();
            if (canDeleteResult.IsFailure)
            {
                return Result.Failure(
                    canDeleteResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            postRepository.Remove(post);

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.MyCreatorPostsTag(profile.Id), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator post deleted: {PostId} (Status={Status}) by creator {CreatorId}",
                post.Id, post.Status, profile.Id);

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
