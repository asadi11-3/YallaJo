using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BlogTranslationEntity = ContentBlogs.Domain.Entities.BlogTranslation;

namespace ContentBlogs.Application.Commands.Blog.UpsertBlogTranslation;

public sealed class UpsertBlogTranslationCommandHandler(
    IBlogRepository blogRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IActiveLanguageProvider activeLanguageProvider,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpsertBlogTranslationCommandHandler> logger)
    : ICommandHandler<UpsertBlogTranslationCommand>
{
    public async Task<Result> Handle(
        UpsertBlogTranslationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var blog = await blogRepository
                .GetByIdAsync(request.BlogId, cancellationToken, asNoTracking: true)
                .ConfigureAwait(false);

            if (blog is null)
                return Result.Failure(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
                    Outcome.NotFound);

            if (blog.IsDeleted)
                return Result.Failure(
                    new Error("Blog.AlreadyDeleted", "This blog has been deleted."),
                    Outcome.UnprocessableEntity);

            var hierarchy = await authorHierarchyGuard
                .EnsureCanManageBlogOwnedByAsync(blog.AuthorId, cancellationToken)
                .ConfigureAwait(false);
            if (!hierarchy.IsSuccess)
                return hierarchy;

            // Resolve language code → active language id at runtime (no hardcoded ids).
            var code = (request.LanguageCode ?? string.Empty).Trim().ToLowerInvariant();
            var languages = await activeLanguageProvider
                .GetActiveLanguagesAsync(cancellationToken)
                .ConfigureAwait(false);

            var language = languages.FirstOrDefault(
                l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
            if (language is null)
                return Result.Failure(
                    new Error("Blog.Translation.LanguageNotActive",
                        $"Language '{request.LanguageCode}' is not active."),
                    Outcome.UnprocessableEntity);

            // Idempotent upsert keyed by (BlogId, LanguageId).
            var existing = await blogRepository
                .GetTranslationAsync(request.BlogId, language.Id, asNoTracking: false, cancellationToken)
                .ConfigureAwait(false);

            bool created;
            if (existing is not null)
            {
                existing.Update(request.Title, request.Content, request.Summary);
                created = false;
            }
            else
            {
                var translation = BlogTranslationEntity.Create(
                    blogId:     blog.Id,
                    languageId: language.Id,
                    title:      request.Title,
                    content:    request.Content,
                    summary:    request.Summary);
                await blogRepository.AddTranslationAsync(translation, cancellationToken).ConfigureAwait(false);
                created = true;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                // Unique index (BlogId, LanguageId) guards against a race that created
                // the row between our read and write — treat as a conflict.
                return Result.Failure(
                    new Error("Blog.Translation.Conflict",
                        "The translation was modified by another request. Please retry."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.BlogTag(blog.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.BlogSlugTag(blog.Slug), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "BlogTranslation upserted: blog {BlogId} language {Code} ({Action}).",
                blog.Id, code, created ? "created" : "updated");

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
