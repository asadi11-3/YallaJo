using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.Posts.RemovePost;

public sealed class RemoveCreatorPostCommandHandler(
    ICreatorPostRepository postRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RemoveCreatorPostCommandHandler> logger)
    : ICommandHandler<RemoveCreatorPostCommand>
{
    public async Task<Result> Handle(
        RemoveCreatorPostCommand request,
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

            var post = await postRepository
                .GetByIdAsync(request.PostId, cancellationToken)
                .ConfigureAwait(false);

            if (post is null)
            {
                return Result.Failure(CreatorPostErrors.NotFound, Outcome.NotFound);
            }

            var removeResult = post.Remove();

            if (removeResult.IsFailure)
            {
                return Result.Failure(
                    removeResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostsListTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostsFeaturedTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.MyCreatorPostsTag(post.CreatorProfileId), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostTag(post.Slug), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator post removed: {PostId} by admin {AdminId}",
                post.Id, currentUser.UserId.Value);

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
