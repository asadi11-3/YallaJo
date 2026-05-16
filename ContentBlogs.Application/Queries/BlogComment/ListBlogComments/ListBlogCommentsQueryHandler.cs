using ContentBlogs.Application.Queries.BlogComment.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.BlogComment.ListBlogComments;

public sealed class ListBlogCommentsQueryHandler(
    IBlogRepository blogRepository,
    IBlogCommentRepository blogCommentRepository,
    ILogger<ListBlogCommentsQueryHandler> logger)
    : IQueryHandler<ListBlogCommentsQuery, PaginatedResult<BlogCommentDto>>
{
    public async Task<Result<PaginatedResult<BlogCommentDto>>> Handle(
        ListBlogCommentsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Blog must exist AND be Published. Draft/Archived are treated as
            // NotFound to anonymous callers (do not leak status).
            var blogExistsAndPublished = await blogRepository
                .AnyAsync(
                    b => b.Id == request.BlogId && b.Status == BlogStatus.Published,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!blogExistsAndPublished)
            {
                return Result<PaginatedResult<BlogCommentDto>>.NotFound(
                    $"Blog '{request.BlogId}' was not found.");
            }

            var paged = await blogCommentRepository
                .SelectPaginatedAsync(
                    pageNumber: request.Page,
                    pageSize: request.PageSize,
                    selector: c => new BlogCommentDto(
                        c.Id,
                        c.BlogId,
                        c.ParentCommentId,
                        c.UserId,
                        c.Content,
                        c.IsContentRedacted,
                        c.LikeCount,
                        c.CreatedAt,
                        c.UpdatedAt),
                    filter: c => c.BlogId == request.BlogId,
                    orderBy: q => q.OrderBy(c => c.CreatedAt),
                    ct: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug(
                "ListBlogComments: blog={BlogId} page={Page} size={Size} total={Total}",
                request.BlogId, request.Page, request.PageSize, paged.TotalCount);

            return Result<PaginatedResult<BlogCommentDto>>.Success(paged);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PaginatedResult<BlogCommentDto>>.Canceled("The request was cancelled.");
        }
    }
}
