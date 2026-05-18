using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Blog.GetDeletedBlogsAdmin;

public sealed class GetDeletedBlogsAdminQueryHandler(
    IBlogRepository blogRepository,
    ILogger<GetDeletedBlogsAdminQueryHandler> logger)
    : IQueryHandler<GetDeletedBlogsAdminQuery, PaginatedResult<AdminDeletedBlogListItemDto>>
{
    private const int MaxPageSize = 100;
    private const string DefaultSortBy = "deletedAt";

    public async Task<Result<PaginatedResult<AdminDeletedBlogListItemDto>>> Handle(
        GetDeletedBlogsAdminQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            var sortBy = NormalizeSortBy(request.SortBy);
            var sortDescending = NormalizeSortOrderIsDescending(request.SortOrder);

            var pagedEntities = await blogRepository.GetDeletedBlogsAsync(
                    page:           page,
                    pageSize:       pageSize,
                    status:         request.Status,
                    placeId:        request.PlaceId,
                    search:         request.Search,
                    sortBy:         sortBy,
                    sortDescending: sortDescending,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var dtos = pagedEntities.Items
                .Select(blog => new AdminDeletedBlogListItemDto(
                    Id:          blog.Id,
                    Slug:        blog.Slug,
                    Title:       blog.Title,
                    Status:      blog.Status.ToString(),
                    IsFeatured:  blog.IsFeatured,
                    PlaceId:     blog.PlaceId,
                    PublishedAt: blog.PublishedAt,
                    DeletedAt:   blog.DeletedAt,
                    UpdatedAt:   blog.UpdatedAt,
                    RowVersion:  blog.RowVersion))
                .ToList()
                .AsReadOnly();

            var pagedDtos = new PaginatedResult<AdminDeletedBlogListItemDto>(
                items:      dtos,
                totalCount: pagedEntities.TotalCount,
                pageNumber: pagedEntities.PageNumber,
                pageSize:   pagedEntities.PageSize);

            logger.LogDebug(
                "GetDeletedBlogsAdmin: page={Page} size={Size} total={Total} " +
                "status={Status} placeId={PlaceId} sortBy={SortBy} sortDesc={SortDesc}",
                page, pageSize, pagedEntities.TotalCount,
                request.Status, request.PlaceId, sortBy, sortDescending);

            return Result<PaginatedResult<AdminDeletedBlogListItemDto>>.Success(pagedDtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PaginatedResult<AdminDeletedBlogListItemDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return DefaultSortBy;

        var trimmed = sortBy.Trim();
        return trimmed switch
        {
            var s when string.Equals(s, "title",     StringComparison.OrdinalIgnoreCase) => "title",
            var s when string.Equals(s, "updatedAt", StringComparison.OrdinalIgnoreCase) => "updatedAt",
            // Default + explicit "deletedAt" + anything else fall through to deletedAt.
            _ => DefaultSortBy,
        };
    }

    private static bool NormalizeSortOrderIsDescending(string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortOrder))
            return true; // default: desc (newest deletions first)

        // "asc" is the only opt-in to ascending; anything else => desc.
        return !string.Equals(sortOrder.Trim(), "asc", StringComparison.OrdinalIgnoreCase);
    }
}
