using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using YallaJo.SharedKernel.Application.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Blog.PublishBlog;

public sealed class PublishBlogCommandHandler(
    IBlogRepository blogRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IActiveLanguageProvider activeLanguageProvider,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<PublishBlogCommandHandler> logger)
    : ICommandHandler<PublishBlogCommand>
{
    public async Task<Result> Handle(
        PublishBlogCommand request,
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
                    "PublishBlog rejected: stale RowVersion for blog {BlogId}.", blog.Id);
                return Result.Failure(
                    new Error(
                        "Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            if (await blogRepository
                .IsSlugReservedAsync(blog.Slug, excludeId: blog.Id, cancellationToken)
                .ConfigureAwait(false))
            {
                return Result.Failure(
                    new Error("Blog.SlugConflict", $"Slug '{blog.Slug}' is no longer available."),
                    Outcome.Conflict);
            }

            // ── AR + EN translation gate (PDF §9) ────────────────────────────
            // Both Arabic and English translations must exist before a blog can
            // be published. English is the source language (always present after
            // Create); Arabic must be explicitly added.
            var activeLanguages = await activeLanguageProvider
                .GetActiveLanguagesAsync(cancellationToken)
                .ConfigureAwait(false);

            var arabicLanguage = activeLanguages.FirstOrDefault(
                l => string.Equals(l.Code, "ar", StringComparison.OrdinalIgnoreCase));

            if (arabicLanguage is not null)
            {
                var hasArabic = await blogRepository
                    .HasTranslationForLanguageAsync(blog.Id, arabicLanguage.Id, cancellationToken)
                    .ConfigureAwait(false);

                if (!hasArabic)
                {
                    return Result.Failure(
                        new Error(
                            "Blog.Translation.ArabicRequired",
                            "An Arabic (ar) translation is required before publishing."),
                        Outcome.UnprocessableEntity);
                }
            }

            try
            {
                blog.Publish(DateTime.UtcNow);
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
            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.SitemapRenderedTag, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Blog published: {BlogId} (Slug={Slug})", blog.Id, blog.Slug);

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
