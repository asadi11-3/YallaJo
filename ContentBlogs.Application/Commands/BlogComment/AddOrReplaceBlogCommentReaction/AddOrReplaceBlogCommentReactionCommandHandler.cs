using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.BlogComment.AddOrReplaceBlogCommentReaction;

public sealed class AddOrReplaceBlogCommentReactionCommandHandler(
    IBlogCommentRepository blogCommentRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<AddOrReplaceBlogCommentReactionCommandHandler> logger)
    : ICommandHandler<AddOrReplaceBlogCommentReactionCommand>
{
    public async Task<Result> Handle(
        AddOrReplaceBlogCommentReactionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Load tracked with reactions: aggregate enforces one-per-user invariant.
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

            try
            {
                comment.AddOrReplaceReaction(currentUser.UserId!.Value, request.ReactionType, utcNow);
            }
            catch (InvalidOperationException ex) when (
                ex.Message.StartsWith("BlogComment.Redacted", StringComparison.Ordinal))
            {
                return Result.Failure(
                    new Error("BlogComment.Redacted", "Redacted comments cannot be reacted to."),
                    Outcome.Conflict);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(
                    new Error("BlogCommentReaction.Invalid", ex.Message),
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
                        "Comment was modified by another user. Please retry."),
                    Outcome.Conflict);
            }
            catch (DbUpdateException)
            {
                // Composite unique index (CommentId, UserId) race: another concurrent
                // request inserted the same user's reaction first.
                return Result.Failure(
                    new Error(
                        "BlogCommentReaction.AlreadyExists",
                        "A reaction for this user already exists. Please retry."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogCommentsTag(comment.BlogId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "BlogCommentReaction add-or-replaced: CommentId={CommentId}, BlogId={BlogId}, " +
                "UserId={UserId}, ReactionType={ReactionType}",
                comment.Id, comment.BlogId, currentUser.UserId!.Value, request.ReactionType);

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
