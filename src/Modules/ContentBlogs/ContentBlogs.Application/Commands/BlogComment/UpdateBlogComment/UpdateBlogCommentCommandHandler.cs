using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using YallaJo.SharedKernel.Application.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.BlogComment.UpdateBlogComment;

public sealed class UpdateBlogCommentCommandHandler(
    IBlogCommentRepository blogCommentRepository,
    IBlogCommentAuthorizationGuard authorizationGuard,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateBlogCommentCommandHandler> logger)
    : ICommandHandler<UpdateBlogCommentCommand>
{
    public async Task<Result> Handle(
        UpdateBlogCommentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Load tracked: we mutate the aggregate below.
            var comment = await blogCommentRepository
                .GetByIdAsync(request.CommentId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (comment is null)
            {
                return Result.Failure(
                    new Error("BlogComment.NotFound", $"Comment '{request.CommentId}' was not found."),
                    Outcome.NotFound);
            }

            var utcNow = DateTime.UtcNow;

            // Row-level authorization (ownership + 30-min window OR moderation hierarchy).
            // IDOR-safe: ownership is derived from the loaded entity, never from the request.
            var authz = await authorizationGuard
                .ResolveEditAuthorizationAsync(comment, utcNow, cancellationToken)
                .ConfigureAwait(false);
            if (!authz.IsSuccess)
                return authz;

            // RowVersion check AFTER authorization (matches Blog handler ordering).
            if (!RowVersionUtil.Equal(comment.RowVersion, request.RowVersion))
            {
                logger.LogWarning(
                    "UpdateBlogComment rejected: stale RowVersion for comment {CommentId}.",
                    comment.Id);
                return Result.Failure(
                    new Error(
                        "BlogComment.ConcurrencyConflict",
                        "This comment was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            try
            {
                comment.Edit(request.Content, utcNow);
            }
            catch (InvalidOperationException ex) when (
                ex.Message.StartsWith("BlogComment.Redacted", StringComparison.Ordinal))
            {
                return Result.Failure(
                    new Error("BlogComment.Redacted", "Redacted comments cannot be edited."),
                    Outcome.Conflict);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(
                    new Error("BlogComment.Invalid", ex.Message),
                    Outcome.Invalid);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "BlogComment.ConcurrencyConflict",
                        "Comment was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogCommentsTag(comment.BlogId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "BlogComment updated: {CommentId} (BlogId={BlogId})",
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
