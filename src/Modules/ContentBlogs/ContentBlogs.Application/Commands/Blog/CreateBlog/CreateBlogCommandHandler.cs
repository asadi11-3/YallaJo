using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Blog.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BlogEntity = ContentBlogs.Domain.Entities.Blog;
using BlogTranslationEntity = ContentBlogs.Domain.Entities.BlogTranslation;

namespace ContentBlogs.Application.Commands.Blog.CreateBlog;

public sealed class CreateBlogCommandHandler(
    IBlogRepository blogRepository,
    ICreatorProfileRepository creatorProfileRepository,
    IActiveLanguageProvider activeLanguageProvider,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateBlogCommandHandler> logger)
    : ICommandHandler<CreateBlogCommand, CreateBlogResult>
{
    private const int AverageWordsPerMinute = 200;

    public async Task<Result<CreateBlogResult>> Handle(
        CreateBlogCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        // ── Resolve source language ──────────────────────────────────────
            var sourceLanguageCode = (request.SourceLanguageCode ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            var activeLanguages = await activeLanguageProvider
                .GetActiveLanguagesAsync(cancellationToken)
                .ConfigureAwait(false);

            var sourceLanguage = activeLanguages.FirstOrDefault(
                lang => string.Equals(lang.Code, sourceLanguageCode, StringComparison.OrdinalIgnoreCase));

            if (sourceLanguage is null)
            {
                logger.LogWarning(
                    "CreateBlog rejected: source language {LanguageCode} is not active.",
                    sourceLanguageCode);
                return Result.Failure<CreateBlogResult>(
                    new Error("Blog.UnknownLanguage", "Source language is not supported."),
                    Outcome.UnprocessableEntity);
            }

            // ── Slug normalization + uniqueness ──────────────────────────────
            var slug = BlogSlugGenerator.Generate(request.Slug, request.Title);
            if (string.IsNullOrWhiteSpace(slug))
            {
                logger.LogWarning(
                    "CreateBlog rejected: slug could not be generated from title '{Title}'.",
                    request.Title);
                return Result.Failure<CreateBlogResult>(
                    new Error(
                        "Blog.SlugInvalid",
                        "A valid slug could not be generated from the supplied title. " +
                        "Provide an explicit ASCII slug (lowercase letters, digits, hyphens)."),
                    Outcome.Invalid);
            }

            if (await blogRepository
                .IsSlugReservedAsync(slug, excludeId: null, cancellationToken)
                .ConfigureAwait(false))
            {
                logger.LogWarning("CreateBlog rejected: slug {Slug} is already reserved.", slug);
                return Result.Failure<CreateBlogResult>(
                    new Error("Blog.SlugConflict", $"A blog with slug '{slug}' already exists."),
                    Outcome.Conflict);
            }

            // ── Build aggregate ──────────────────────────────────────────────
            var utcNow = DateTime.UtcNow;
            var readTimeMinutes = EstimateReadTimeMinutes(request.Content);

            var authorId = currentUser.UserId!.Value;

            var creatorProfile = await creatorProfileRepository
                .GetByUserIdAsync(authorId, cancellationToken)
                .ConfigureAwait(false);

            var blog = creatorProfile is not null
                ? BlogEntity.CreateByCreator(
                    title:             request.Title,
                    slug:              slug,
                    content:           request.Content,
                    authorId:          authorId,
                    creatorProfileId:  creatorProfile.Id,
                    sourceLanguageId:  sourceLanguage.Id,
                    utcNow:            utcNow,
                    summary:           request.Summary,
                    metaTitle:         request.MetaTitle,
                    metaDescription:   request.MetaDescription,
                    placeId:           request.PlaceId,
                    readTimeMinutes:   readTimeMinutes)
                : BlogEntity.Create(
                    title:            request.Title,
                    slug:             slug,
                    content:          request.Content,
                    authorId:         authorId,
                    sourceLanguageId: sourceLanguage.Id,
                    utcNow:           utcNow,
                    summary:          request.Summary,
                    metaTitle:        request.MetaTitle,
                    metaDescription: request.MetaDescription,
                    placeId:          request.PlaceId,
                    readTimeMinutes:  readTimeMinutes);

            // Source-language translation in the same unit of work.
            var sourceTranslation = BlogTranslationEntity.Create(
                blogId:     blog.Id,
                languageId: sourceLanguage.Id,
                title:      blog.Title,
                content:    blog.Content,
                summary:    blog.Summary);
            blog.AddTranslation(sourceTranslation);

            await blogRepository.AddAsync(blog, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<CreateBlogResult>(
                    new Error(
                        "Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.BlogsListTag, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Blog created: {BlogId} (Slug={Slug}, AuthorId={AuthorId}, AuthoredByCreatorId={CreatorId})",
                blog.Id, blog.Slug, blog.AuthorId, blog.AuthoredByCreatorId);

            return Result.Created(new CreateBlogResult(blog.Id, blog.Slug));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<CreateBlogResult>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    /// <summary>
    /// Estimates reading time in whole minutes.  HTML tags are stripped first
    /// so a content body like <c>"&lt;p&gt;hello&lt;/p&gt;"</c> counts one word,
    /// not three tokens.
    /// </summary>
    private static int EstimateReadTimeMinutes(string content)
    {
        var words = BlogContentTextHelper.CountWords(content);
        if (words == 0)
            return 1;

        var minutes = (int)Math.Ceiling(words / (double)AverageWordsPerMinute);
        return Math.Max(1, minutes);
    }
}
