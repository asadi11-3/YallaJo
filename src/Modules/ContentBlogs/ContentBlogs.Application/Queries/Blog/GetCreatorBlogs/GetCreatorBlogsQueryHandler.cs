using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Blog.GetCreatorBlogs;

public sealed class GetCreatorBlogsQueryHandler(
    IBlogRepository blogRepository,
    ILogger<GetCreatorBlogsQueryHandler> logger)
    : IQueryHandler<GetCreatorBlogsQuery, PaginatedResult<BlogSummaryDto>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PaginatedResult<BlogSummaryDto>>> Handle(
        GetCreatorBlogsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            var paginatedBlogs = await blogRepository.GetPublishedByCreatorProfileIdAsync(
                request.CreatorProfileId, page, pageSize, cancellationToken).ConfigureAwait(false);

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

            var result = new PaginatedResult<BlogSummaryDto>(
                dtos, paginatedBlogs.TotalCount, page, pageSize);

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
