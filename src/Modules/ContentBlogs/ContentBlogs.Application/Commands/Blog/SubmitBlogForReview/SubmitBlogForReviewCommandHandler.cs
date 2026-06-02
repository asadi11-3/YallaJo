using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using YallaJo.SharedKernel.Application.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Blog.SubmitBlogForReview;

public sealed class SubmitBlogForReviewCommandHandler(
    IBlogRepository blogRepository,
    ICurrentUser currentUser,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<SubmitBlogForReviewCommandHandler> logger)
    : ICommandHandler<SubmitBlogForReviewCommand>
{
    public async Task<Result> Handle(
        SubmitBlogForReviewCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var blog = await blogRepository
                .GetByIdAsync(request.BlogId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (blog is null)
            {
                return Result.Failure(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
                    Outcome.NotFound);
            }

            // Ensure the caller owns this blog
            if (blog.AuthorId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    new Error("Blog.Forbidden", "You do not have permission to submit this blog."),
                    Outcome.Forbidden);
            }

            if (!RowVersionUtil.Equal(blog.RowVersion, request.RowVersion))
            {
                return Result.Failure(
                    new Error("Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            var submitResult = blog.SubmitForReview(DateTime.UtcNow);
            if (!submitResult.IsSuccess)
            {
                return submitResult;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.BlogTag(blog.Id), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Blog submitted for review: {BlogId} (Slug={Slug}) by creator {UserId}",
                blog.Id, blog.Slug, currentUser.UserId!.Value);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
