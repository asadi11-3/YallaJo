using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.Posts.RejectPost;

public sealed class RejectCreatorPostCommandHandler(
    ICreatorPostRepository postRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RejectCreatorPostCommandHandler> logger)
    : ICommandHandler<RejectCreatorPostCommand>
{
    public async Task<Result> Handle(
        RejectCreatorPostCommand request,
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

            var rejectResult = post.RejectByAdmin(
                currentUser.UserId!.Value, request.Reason, DateTime.UtcNow);

            if (rejectResult.IsFailure)
            {
                return Result.Failure(
                    rejectResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.AdminPostsQueueTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.MyCreatorPostsTag(post.CreatorProfileId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator post rejected: {PostId} by admin {AdminId}",
                post.Id, currentUser.UserId!.Value);

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
