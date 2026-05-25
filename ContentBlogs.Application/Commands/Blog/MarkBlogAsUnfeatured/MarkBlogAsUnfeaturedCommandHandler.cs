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

namespace ContentBlogs.Application.Commands.Blog.MarkBlogAsUnfeatured;

public sealed class MarkBlogAsUnfeaturedCommandHandler(
    IBlogRepository blogRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<MarkBlogAsUnfeaturedCommandHandler> logger)
    : ICommandHandler<MarkBlogAsUnfeaturedCommand>
{
    public async Task<Result> Handle(
        MarkBlogAsUnfeaturedCommand request,
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

            var hierarchy = await authorHierarchyGuard
                .EnsureCanManageBlogOwnedByAsync(blog.AuthorId, cancellationToken)
                .ConfigureAwait(false);
            if (!hierarchy.IsSuccess)
            {
                return hierarchy;
            }

            if (!RowVersionUtil.Equal(blog.RowVersion, request.RowVersion))
            {
                logger.LogWarning(
                    "MarkBlogAsUnfeatured rejected: stale RowVersion for blog {BlogId}.", blog.Id);
                return Result.Failure(
                    new Error(
                        "Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            // TODO: Phase 4 replaces this handler entirely with UnfeatureBlogCommandHandler
            var unfeatureResult = blog.Unfeature(DateTime.UtcNow);
            if (!unfeatureResult.IsSuccess)
            {
                return unfeatureResult;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogTag(blog.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogSlugTag(blog.Slug), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogsListTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.SitemapRenderedTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.FeaturedBlogsTag, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Blog feature removed: {BlogId} (Slug={Slug}, PlaceId={PlaceId})",
                blog.Id, blog.Slug, blog.PlaceId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private static Result MapDomainGuardFailure(InvalidOperationException ex)
    {
        var message = ex.Message ?? string.Empty;

        if (message.StartsWith("Blog.Deleted", StringComparison.Ordinal))
        {
            return Result.Failure(
                new Error("Blog.NotFound", "Blog was not found."),
                Outcome.NotFound);
        }

        return Result.Failure(
            new Error("Blog.InvalidTransition", message),
            Outcome.Conflict);
    }
}
