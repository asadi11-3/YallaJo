using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Blog.GetMyBlogs;

public sealed class GetMyBlogsQueryHandler(
    IBlogRepository blogRepository,
    ICurrentUser currentUser,
    IActiveLanguageProvider activeLanguageProvider,
    HybridCache cache,
    ILogger<GetMyBlogsQueryHandler> logger)
    : IQueryHandler<GetMyBlogsQuery, PaginatedResult<BlogSummaryDto>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PaginatedResult<BlogSummaryDto>>> Handle(
        GetMyBlogsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.UserId!.Value;
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            BlogStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
                Enum.TryParse<BlogStatus>(request.StatusFilter, ignoreCase: true, out var parsed))
            {
                statusFilter = parsed;
            }

            // Include the EFFECTIVE (normalized) status in the cache key so different
            // status filters never return each other's cached results. An unrecognized
            // filter normalizes to null (= "all"), matching the actual query behavior.
            var cacheKey = ContentBlogsCacheKeys.MyBlogs(userId, page, pageSize, statusFilter?.ToString());
            var cacheTag = ContentBlogsCacheKeys.MyBlogsTag(userId);

            var result = await cache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    var paginatedBlogs = await blogRepository.GetByAuthorIdAsync(
                        userId, page, pageSize, statusFilter, ct).ConfigureAwait(false);

                    // Map the article's source LanguageId → language code for display in
                    // the creator's My Articles list (Gap 1). Falls back to null when the
                    // language is unknown/inactive so the consumer can default safely.
                    var activeLanguages = await activeLanguageProvider
                        .GetActiveLanguagesAsync(ct).ConfigureAwait(false);
                    var languageCodeById = activeLanguages
                        .ToDictionary(l => l.Id, l => l.Code);

                    var dtos = paginatedBlogs.Items.Select(b => new BlogSummaryDto(
                        Id: b.Id,
                        Slug: b.Slug,
                        Title: b.Title,
                        Summary: b.Summary,
                        PublishedAt: b.PublishedAt,
                        ViewCount: b.ViewCount,
                        ReadTimeMinutes: b.ReadTimeMinutes,
                        PlaceId: b.PlaceId,
                        LanguageCode: "default",
                        IsFeatured: b.IsFeatured,
                        Status: b.Status.ToString(),
                        SourceLanguageCode: languageCodeById.TryGetValue(b.LanguageId, out var code)
                            ? code
                            : null)).ToList();

                    return new PaginatedResult<BlogSummaryDto>(
                        dtos, paginatedBlogs.TotalCount, page, pageSize);
                },
                new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
                tags: [cacheTag],
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return Result<PaginatedResult<BlogSummaryDto>>.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PaginatedResult<BlogSummaryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
