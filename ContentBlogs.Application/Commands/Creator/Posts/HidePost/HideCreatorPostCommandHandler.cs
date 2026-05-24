using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.Posts.HidePost;

public sealed class HideCreatorPostCommandHandler(
    ICreatorPostRepository postRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<HideCreatorPostCommandHandler> logger)
    : ICommandHandler<HideCreatorPostCommand>
{
    public async Task<Result> Handle(
        HideCreatorPostCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await postRepository
                .GetByIdAsync(request.PostId, cancellationToken)
                .ConfigureAwait(false);

            if (post is null)
            {
                return Result.Failure(CreatorPostErrors.NotFound, Outcome.NotFound);
            }

            var hideResult = post.Hide(currentUser.UserId!.Value, request.Reason, DateTime.UtcNow);

            if (hideResult.IsFailure)
            {
                return Result.Failure(
                    hideResult.Errors.FirstOrDefault()!,
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
                "Creator post hidden: {PostId} by admin {AdminId}, reason: {Reason}",
                post.Id, currentUser.UserId!.Value, request.Reason);

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
