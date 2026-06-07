using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.BlogComment.RemoveBlogCommentReaction;

public sealed class RemoveBlogCommentReactionCommandHandler(
    IBlogCommentRepository blogCommentRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RemoveBlogCommentReactionCommandHandler> logger)
    : ICommandHandler<RemoveBlogCommentReactionCommand>
{
    public async Task<Result> Handle(
        RemoveBlogCommentReactionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure(
                    new Error("BlogCommentReaction.Unauthorized", "Authentication is required to react to comments."),
                    Outcome.Unauthorized);
            }

            var comment = await blogCommentRepository
                .GetWithReactionsAsync(request.CommentId, cancellationToken)
                .ConfigureAwait(false);

            if (comment is null)
            {
                return Result.Failure(
                    new Error("BlogComment.NotFound", $"Comment '{request.CommentId}' was not found."),
                    Outcome.NotFound);
            }

            var utcNow = DateTime.UtcNow;
            var removed = comment.RemoveReaction(currentUser.UserId.Value, utcNow);

            if (removed)
            {
                try
                {
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Result.Failure(
                        new Error(
                            "BlogComment.ConcurrencyConflict",
                            "Comment was modified by another user. Please retry."),
                        Outcome.Conflict);
                }

                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.BlogCommentsTag(comment.BlogId), cancellationToken)
                    .ConfigureAwait(false);

                logger.LogInformation(
                    "BlogCommentReaction removed: CommentId={CommentId}, BlogId={BlogId}, UserId={UserId}",
                    comment.Id, comment.BlogId, currentUser.UserId.Value);
            }

            // Idempotent: success regardless of whether anything was actually removed.
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
