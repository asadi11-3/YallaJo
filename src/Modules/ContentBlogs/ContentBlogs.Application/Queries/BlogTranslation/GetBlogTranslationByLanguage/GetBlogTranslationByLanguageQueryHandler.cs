using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Queries.BlogTranslation.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.BlogTranslation.GetBlogTranslationByLanguage;

public sealed class GetBlogTranslationByLanguageQueryHandler(
    IBlogRepository blogRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<GetBlogTranslationByLanguageQueryHandler> logger)
    : IQueryHandler<GetBlogTranslationByLanguageQuery, BlogTranslationAdminDto>
{
    public async Task<Result<BlogTranslationAdminDto>> Handle(
        GetBlogTranslationByLanguageQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var blog = await blogRepository
                .GetByIdAsync(request.BlogId, cancellationToken, asNoTracking: true)
                .ConfigureAwait(false);

            if (blog is null)
                return Result.Failure<BlogTranslationAdminDto>(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
                    Outcome.NotFound);

            var hierarchy = await authorHierarchyGuard
                .EnsureCanManageBlogOwnedByAsync(blog.AuthorId, cancellationToken)
                .ConfigureAwait(false);
            if (!hierarchy.IsSuccess)
                return Result.Failure<BlogTranslationAdminDto>(
                    hierarchy.Errors.Count > 0 ? hierarchy.Errors[0]
                        : new Error("Blog.AuthorHierarchyForbidden", "You cannot manage this blog."),
                    hierarchy.Outcome);

            var code = (request.LanguageCode ?? string.Empty).Trim().ToLowerInvariant();
            var languages = await activeLanguageProvider
                .GetActiveLanguagesAsync(cancellationToken)
                .ConfigureAwait(false);

            var language = languages.FirstOrDefault(
                l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
            if (language is null)
                return Result.Failure<BlogTranslationAdminDto>(
                    new Error("Blog.Translation.LanguageNotActive",
                        $"Language '{request.LanguageCode}' is not active."),
                    Outcome.UnprocessableEntity);

            var translation = await blogRepository
                .GetTranslationAsync(request.BlogId, language.Id, asNoTracking: true, cancellationToken)
                .ConfigureAwait(false);

            if (translation is null)
                return Result.Failure<BlogTranslationAdminDto>(
                    new Error("Blog.Translation.NotFound",
                        $"No '{code}' translation exists for this blog."),
                    Outcome.NotFound);

            return Result.Success(new BlogTranslationAdminDto(
                Id:           translation.Id,
                BlogId:       translation.BlogId,
                LanguageId:   translation.LanguageId,
                LanguageCode: language.Code,
                Title:        translation.Title,
                Content:      translation.Content,
                Summary:      translation.Summary));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<BlogTranslationAdminDto>(
                new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
