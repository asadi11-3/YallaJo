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

namespace ContentBlogs.Application.Commands.Blog.RestoreBlog;

public sealed class RestoreBlogCommandHandler(
    IBlogRepository blogRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<RestoreBlogCommandHandler> logger)
    : ICommandHandler<RestoreBlogCommand>
{
    public async Task<Result> Handle(
        RestoreBlogCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var blog = await blogRepository
                .GetByIdIncludingDeletedAsync(request.BlogId, cancellationToken)
                .ConfigureAwait(false);

            if (blog is null)
            {
                return Result.Failure(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
                    Outcome.NotFound);
            }

            if (!blog.IsDeleted)
            {
                return Result.Failure(
                    new Error(
                        "Blog.InvalidTransition",
                        "Blog is not deleted; nothing to restore."),
                    Outcome.Conflict);
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
                    "RestoreBlog rejected: stale RowVersion for blog {BlogId}.", blog.Id);
                return Result.Failure(
                    new Error(
                        "Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            if (blog.IsFeatured)
            {
                var conflicting = await blogRepository
                    .GetFeaturedBlogInPlaceScopeAsync(
                        blog.PlaceId, excludeBlogId: blog.Id, cancellationToken)
                    .ConfigureAwait(false);
                if (conflicting is not null)
                {
                    logger.LogWarning(
                        "RestoreBlog rejected: another blog {ConflictingBlogId} is already " +
                        "featured in PlaceId scope {PlaceId}.",
                        conflicting.Id, blog.PlaceId);
                    return Result.Failure(
                        new Error(
                            "Blog.FeaturedConflict",
                            blog.PlaceId.HasValue
                                ? $"Another blog is already featured for place '{blog.PlaceId}'. " +
                                  "Clear it first, or clear this blog before restoring."
                                : "Another blog is already featured in the global spotlight slot. " +
                                  "Clear it first, or clear this blog before restoring."),
                        Outcome.Conflict);
                }
            }

            try
            {
                blog.Restore(DateTime.UtcNow);
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

            if (blog.IsFeatured)
            {
                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.FeaturedBlogsTag, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (blog.PlaceId.HasValue)
            {
                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.BlogPlaceTag(blog.PlaceId.Value), cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "Blog restored: {BlogId} (Slug={Slug}, Status={Status}, " +
                "IsFeatured={IsFeatured}, PlaceId={PlaceId})",
                blog.Id, blog.Slug, blog.Status, blog.IsFeatured, blog.PlaceId);

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
