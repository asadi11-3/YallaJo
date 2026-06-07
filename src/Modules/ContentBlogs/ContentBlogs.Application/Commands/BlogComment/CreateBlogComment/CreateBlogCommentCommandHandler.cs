using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BlogCommentEntity = ContentBlogs.Domain.Entities.BlogComment;

namespace ContentBlogs.Application.Commands.BlogComment.CreateBlogComment;

public sealed class CreateBlogCommentCommandHandler(
    IBlogRepository blogRepository,
    IBlogCommentRepository blogCommentRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateBlogCommentCommandHandler> logger)
    : ICommandHandler<CreateBlogCommentCommand, CreateBlogCommentResult>
{
    public async Task<Result<CreateBlogCommentResult>> Handle(
        CreateBlogCommentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result<CreateBlogCommentResult>.Failure(
                    new Error("BlogComment.Unauthorized", "Authentication is required to create comments."),
                    Outcome.Unauthorized);
            }

            // ── Blog must exist (and we need its status). Read-only.
            var blog = await blogRepository
                .GetByIdAsync(request.BlogId, cancellationToken, asNoTracking: true)
                .ConfigureAwait(false);

            if (blog is null)
            {
                return Result<CreateBlogCommentResult>.Failure(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
                    Outcome.NotFound);
            }

            if (blog.Status != BlogStatus.Published)
            {
                logger.LogInformation(
                    "CreateBlogComment rejected: blog {BlogId} status is {Status} (not Published).",
                    blog.Id, blog.Status);
                return Result<CreateBlogCommentResult>.Failure(
                    new Error(
                        "BlogComment.BlogNotAcceptingComments",
                        "Comments are only allowed on Published blogs."),
                    Outcome.Conflict);
            }

            // ── Parent comment validation (existence + same-blog + depth).
            BlogCommentEntity? parent = null;
            if (request.ParentCommentId.HasValue)
            {
                parent = await blogCommentRepository
                    .GetWithParentChainAsync(request.ParentCommentId.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (parent is null)
                {
                    return Result<CreateBlogCommentResult>.Failure(
                        new Error(
                            "BlogComment.ParentNotFound",
                            $"Parent comment '{request.ParentCommentId.Value}' was not found."),
                        Outcome.NotFound);
                }

                // IDOR / cross-blog reply defense: re-derive truth from DB and compare
                // against the route-supplied BlogId.
                if (parent.BlogId != request.BlogId)
                {
                    logger.LogWarning(
                        "CreateBlogComment rejected: parent {ParentId} belongs to blog {ParentBlogId}, " +
                        "not the requested blog {RequestedBlogId}.",
                        parent.Id, parent.BlogId, request.BlogId);
                    return Result<CreateBlogCommentResult>.Failure(
                        new Error(
                            "BlogComment.ParentBlogMismatch",
                            "Parent comment does not belong to the specified blog."),
                        Outcome.Invalid);
                }
            }

            // ── Build aggregate (domain enforces all invariants again).
            var utcNow = DateTime.UtcNow;
            BlogCommentEntity comment;
            try
            {
                comment = BlogCommentEntity.Create(
                    blogId: request.BlogId,
                    userId: currentUser.UserId.Value,
                    content: request.Content,
                    parent: parent,
                    blogStatus: blog.Status,
                    utcNow: utcNow);
            }
            catch (InvalidOperationException ex)
            {
                return MapDomainGuardFailure(ex);
            }
            catch (ArgumentException ex)
            {
                return Result<CreateBlogCommentResult>.Failure(
                    new Error("BlogComment.Invalid", ex.Message),
                    Outcome.Invalid);
            }

            await blogCommentRepository.AddAsync(comment, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<CreateBlogCommentResult>.Failure(
                    new Error(
                        "BlogComment.ConcurrencyConflict",
                        "Comment could not be saved due to a concurrency conflict. Please retry."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogCommentsTag(request.BlogId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "BlogComment created: {CommentId} (BlogId={BlogId}, ParentId={ParentId}, UserId={UserId})",
                comment.Id, comment.BlogId, comment.ParentCommentId, comment.UserId);

            return Result.Created(new CreateBlogCommentResult(comment.Id));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreateBlogCommentResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private static Result<CreateBlogCommentResult> MapDomainGuardFailure(InvalidOperationException ex)
    {
        var message = ex.Message ?? string.Empty;

        if (message.StartsWith("BlogComment.MaxDepthExceeded", StringComparison.Ordinal))
        {
            return Result<CreateBlogCommentResult>.Failure(
                new Error("BlogComment.MaxDepthExceeded", message),
                Outcome.Conflict);
        }

        if (message.StartsWith("BlogComment.ParentBlogMismatch", StringComparison.Ordinal))
        {
            return Result<CreateBlogCommentResult>.Failure(
                new Error("BlogComment.ParentBlogMismatch", message),
                Outcome.Invalid);
        }

        if (message.StartsWith("BlogComment.BlogNotPublished", StringComparison.Ordinal))
        {
            return Result<CreateBlogCommentResult>.Failure(
                new Error("BlogComment.BlogNotAcceptingComments", message),
                Outcome.Conflict);
        }

        return Result<CreateBlogCommentResult>.Failure(
            new Error("BlogComment.Invalid", message),
            Outcome.Invalid);
    }
}
