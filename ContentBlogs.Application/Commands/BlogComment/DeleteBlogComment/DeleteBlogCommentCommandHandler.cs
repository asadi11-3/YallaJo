using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.BlogComment.DeleteBlogComment;

public sealed class DeleteBlogCommentCommandHandler(
    IBlogCommentRepository blogCommentRepository,
    IBlogCommentAuthorizationGuard authorizationGuard,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeleteBlogCommentCommandHandler> logger)
    : ICommandHandler<DeleteBlogCommentCommand>
{
    public async Task<Result> Handle(
        DeleteBlogCommentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var comment = await blogCommentRepository
                .GetByIdAsync(request.CommentId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (comment is null)
            {
                return Result.Failure(
                    new Error("BlogComment.NotFound", $"Comment '{request.CommentId}' was not found."),
                    Outcome.NotFound);
            }

            // IDOR-safe: ownership derived from the loaded entity.
            var authz = await authorizationGuard
                .ResolveDeleteAuthorizationAsync(comment, cancellationToken)
                .ConfigureAwait(false);
            if (!authz.IsSuccess)
                return authz;

            // RowVersion check AFTER authorization (matches Blog handler ordering).
            if (!RowVersionUtil.Equal(comment.RowVersion, request.RowVersion))
            {
                logger.LogWarning(
                    "DeleteBlogComment rejected: stale RowVersion for comment {CommentId}.",
                    comment.Id);
                return Result.Failure(
                    new Error(
                        "BlogComment.ConcurrencyConflict",
                        "This comment was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            // Idempotent: domain short-circuits if already redacted.
            comment.Redact(DateTime.UtcNow);

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
                "BlogComment soft-deleted (redacted): {CommentId} (BlogId={BlogId})",
                comment.Id, comment.BlogId);

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
