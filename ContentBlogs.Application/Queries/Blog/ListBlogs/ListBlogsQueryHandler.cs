using ContentBlogs.Application.Queries.Blog.Common;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BlogEntity = ContentBlogs.Domain.Entities.Blog;

namespace ContentBlogs.Application.Queries.Blog.ListBlogs;

public sealed class ListBlogsQueryHandler(
    IBlogRepository blogRepository,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<ListBlogsQueryHandler> logger)
    : IQueryHandler<ListBlogsQuery, PaginatedResult<BlogSummaryDto>>
{
    private const int MaxPageSize = 100;
    private const string DefaultLanguageMarker = "default";

    public async Task<Result<PaginatedResult<BlogSummaryDto>>> Handle(
        ListBlogsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            var resolvedLanguage = await AcceptLanguageResolver.ResolveAsync(
                request.AcceptLanguage, activeLanguageProvider, cancellationToken)
                .ConfigureAwait(false);

            var targetLanguageId = resolvedLanguage?.Id;
            var resolvedLanguageCode = resolvedLanguage?.Code ?? DefaultLanguageMarker;

            var search = string.IsNullOrWhiteSpace(request.Search)
                ? null
                : request.Search.Trim().ToLowerInvariant();

            var paged = await blogRepository
                .SelectPaginatedAsync(
                    pageNumber: page,
                    pageSize:   pageSize,
                    selector:   blog => new BlogSummaryDto(
                        blog.Id,
                        blog.Slug,
                        targetLanguageId == null
                            ? blog.Title
                            : (blog.BlogTranslations
                                .Where(t => t.LanguageId == targetLanguageId.Value)
                                .Select(t => t.Title)
                                .FirstOrDefault() ?? blog.Title),
                        targetLanguageId == null
                            ? blog.Summary
                            : (blog.BlogTranslations
                                .Where(t => t.LanguageId == targetLanguageId.Value)
                                .Select(t => t.Summary)
                                .FirstOrDefault() ?? blog.Summary),
                        blog.PublishedAt,
                        blog.ViewCount,
                        blog.ReadTimeMinutes,
                        blog.PlaceId,
                        resolvedLanguageCode),
                    filter: blog => blog.Status == BlogStatus.Published
                        && (request.PlaceId == null || blog.PlaceId == request.PlaceId)
                        && (search == null
                            || blog.Slug.Contains(search)
                            || blog.Title.Contains(search)
                            || (blog.Summary != null && blog.Summary.Contains(search))),
                    orderBy: q => q.OrderByDescending(b => b.PublishedAt)
                                   .ThenByDescending(b => b.CreatedAt),
                    ct: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug(
                "ListBlogs: page={Page} size={Size} total={Total} lang={Lang}",
                page, pageSize, paged.TotalCount, resolvedLanguageCode);

            return Result<PaginatedResult<BlogSummaryDto>>.Success(paged);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PaginatedResult<BlogSummaryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
