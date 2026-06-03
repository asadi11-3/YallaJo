using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Queries.BlogTranslation.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.BlogTranslation.GetBlogTranslations;

public sealed class GetBlogTranslationsQueryHandler(
    IBlogRepository blogRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<GetBlogTranslationsQueryHandler> logger)
    : IQueryHandler<GetBlogTranslationsQuery, IReadOnlyList<BlogTranslationAdminDto>>
{
    public async Task<Result<IReadOnlyList<BlogTranslationAdminDto>>> Handle(
        GetBlogTranslationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var blog = await blogRepository
                .GetByIdAsync(request.BlogId, cancellationToken, asNoTracking: true)
                .ConfigureAwait(false);

            if (blog is null)
                return Result.Failure<IReadOnlyList<BlogTranslationAdminDto>>(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
                    Outcome.NotFound);

            var hierarchy = await authorHierarchyGuard
                .EnsureCanManageBlogOwnedByAsync(blog.AuthorId, cancellationToken)
                .ConfigureAwait(false);
            if (!hierarchy.IsSuccess)
                return Result.Failure<IReadOnlyList<BlogTranslationAdminDto>>(
                    hierarchy.Errors.Count > 0 ? hierarchy.Errors[0]
                        : new Error("Blog.AuthorHierarchyForbidden", "You cannot manage this blog."),
                    hierarchy.Outcome);

            var languages = await activeLanguageProvider
                .GetActiveLanguagesAsync(cancellationToken)
                .ConfigureAwait(false);
            var codeById = languages.ToDictionary(l => l.Id, l => l.Code);

            var translations = await blogRepository
                .GetTranslationsAsync(request.BlogId, cancellationToken)
                .ConfigureAwait(false);

            var dtos = translations
                .Select(t => new BlogTranslationAdminDto(
                    Id:           t.Id,
                    BlogId:       t.BlogId,
                    LanguageId:   t.LanguageId,
                    LanguageCode: codeById.TryGetValue(t.LanguageId, out var code) ? code : string.Empty,
                    Title:        t.Title,
                    Content:      t.Content,
                    Summary:      t.Summary))
                .ToList();

            return Result.Success<IReadOnlyList<BlogTranslationAdminDto>>(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<IReadOnlyList<BlogTranslationAdminDto>>(
                new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
