using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Blog.GetMyBlogs;

public sealed class GetMyBlogsQueryHandler(
    IBlogRepository blogRepository,
    ICurrentUser currentUser,
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

            var cacheKey = ContentBlogsCacheKeys.MyBlogs(userId, page, pageSize);
            var cacheTag = ContentBlogsCacheKeys.MyBlogsTag(userId);

            var result = await cache.GetOrCreateAsync(
                cacheKey,
                async ct =>
                {
                    var paginatedBlogs = await blogRepository.GetByAuthorIdAsync(
                        userId, page, pageSize, statusFilter, ct).ConfigureAwait(false);

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
                        IsFeatured: b.IsFeatured)).ToList();

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
