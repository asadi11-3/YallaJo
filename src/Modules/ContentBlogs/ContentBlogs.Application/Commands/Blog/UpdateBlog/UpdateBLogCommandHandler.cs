using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Blog.Common;
using YallaJo.SharedKernel.Application.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Blog.UpdateBlog;

public sealed class UpdateBlogCommandHandler(
    IBlogRepository blogRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateBlogCommandHandler> logger)
    : ICommandHandler<UpdateBlogCommand>
{
    public async Task<Result> Handle(
        UpdateBlogCommand request,
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
                    "UpdateBlog rejected: stale RowVersion for blog {BlogId}.", blog.Id);
                return Result.Failure(
                    new Error(
                        "Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            var oldSlug = blog.Slug;
            var newSlug = BlogSlugGenerator.Generate(request.Slug, request.Title);

            if (string.IsNullOrWhiteSpace(newSlug))
            {
                logger.LogWarning(
                    "UpdateBlog rejected: slug could not be normalized for blog {BlogId}.", blog.Id);
                return Result.Failure(
                    new Error(
                        "Blog.SlugInvalid",
                        "Slug could not be normalized to a valid URL-safe token."),
                    Outcome.Invalid);
            }

            var slugChanged = !string.Equals(oldSlug, newSlug, StringComparison.Ordinal);

            if (slugChanged && await blogRepository
                .IsSlugReservedAsync(newSlug, excludeId: blog.Id, cancellationToken)
                .ConfigureAwait(false))
            {
                logger.LogWarning(
                    "UpdateBlog rejected: slug {NewSlug} already reserved by another blog.", newSlug);
                return Result.Failure(
                    new Error("Blog.SlugConflict", $"A blog with slug '{newSlug}' already exists."),
                    Outcome.Conflict);
            }

            var utcNow = DateTime.UtcNow;
            var wasPublished = blog.Status == Domain.Enums.BlogStatus.Published;

            try
            {
                blog.Update(
                    title:           request.Title,
                    slug:            newSlug,
                    content:         request.Content,
                    summary:         request.Summary,
                    metaTitle:       request.MetaTitle,
                    metaDescription: request.MetaDescription,
                    placeId:         request.PlaceId,
                    readTimeMinutes: request.ReadTimeMinutes,
                    utcNow:          utcNow);
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

            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.BlogTag(blog.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.BlogsListTag, cancellationToken)
                .ConfigureAwait(false);

            if (slugChanged)
            {
                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.BlogSlugTag(oldSlug), cancellationToken)
                    .ConfigureAwait(false);
                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.BlogSlugTag(newSlug), cancellationToken)
                    .ConfigureAwait(false);
            }

            if (slugChanged && wasPublished)
            {
                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.SitemapRenderedTag, cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "Blog updated: {BlogId} (OldSlug={OldSlug}, NewSlug={NewSlug})",
                blog.Id, oldSlug, newSlug);

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

        if (message.StartsWith("Blog.ArchivedReadOnly", StringComparison.Ordinal))
        {
            return Result.Failure(
                new Error("Blog.ArchivedReadOnly", "Archived blogs cannot be modified."),
                Outcome.Conflict);
        }

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
