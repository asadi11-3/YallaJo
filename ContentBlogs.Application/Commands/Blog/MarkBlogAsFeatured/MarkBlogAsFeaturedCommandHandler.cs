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

namespace ContentBlogs.Application.Commands.Blog.MarkBlogAsFeatured;

public sealed class MarkBlogAsFeaturedCommandHandler(
    IBlogRepository blogRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<MarkBlogAsFeaturedCommandHandler> logger)
    : ICommandHandler<MarkBlogAsFeaturedCommand>
{
    public async Task<Result> Handle(
        MarkBlogAsFeaturedCommand request,
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
                    "MarkBlogAsFeatured rejected: stale RowVersion for blog {BlogId}.", blog.Id);
                return Result.Failure(
                    new Error(
                        "Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            var conflicting = await blogRepository
                .GetFeaturedBlogInPlaceScopeAsync(
                    blog.PlaceId, excludeBlogId: blog.Id, cancellationToken)
                .ConfigureAwait(false);
            if (conflicting is not null)
            {
                logger.LogWarning(
                    "MarkBlogAsFeatured rejected: another blog {ConflictingBlogId} is already featured " +
                    "in PlaceId scope {PlaceId}.",
                    conflicting.Id, blog.PlaceId);
                return Result.Failure(
                new Error(
                    "Blog.FeaturedConflict",
                    blog.PlaceId.HasValue ? $"Another blog is already marked as featured for place '{blog.PlaceId}'. " +
                    "Clear its featured status first."
            :       "Another blog is already marked as featured in the global spotlight slot. " +
                    "Clear its featured status first."), Outcome.Conflict);
            }

            try
            {
                blog.MarkAsFeatured(DateTime.UtcNow);
            }
            catch (InvalidOperationException ex)
            {
                return MapDomainGuardFailure(ex);
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
                "Blog featured: {BlogId} (Slug={Slug}, PlaceId={PlaceId})",
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
