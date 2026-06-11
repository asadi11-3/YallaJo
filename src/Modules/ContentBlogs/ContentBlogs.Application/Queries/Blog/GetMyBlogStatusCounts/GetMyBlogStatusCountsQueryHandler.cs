using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Blog.GetMyBlogStatusCounts;

public sealed class GetMyBlogStatusCountsQueryHandler(
    IBlogRepository blogRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyBlogStatusCountsQuery, MyBlogStatusCountsDto>
{
    public async Task<Result<MyBlogStatusCountsDto>> Handle(
        GetMyBlogStatusCountsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.UserId!.Value;

            var counts = await blogRepository
                .GetStatusCountsByAuthorIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);
            var deleted = await blogRepository
                .CountDeletedByAuthorIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            var dto = new MyBlogStatusCountsDto(
                Draft: counts.GetValueOrDefault(BlogStatus.Draft),
                PendingReview: counts.GetValueOrDefault(BlogStatus.PendingReview),
                Published: counts.GetValueOrDefault(BlogStatus.Published),
                Archived: counts.GetValueOrDefault(BlogStatus.Archived),
                Deleted: deleted);

            return Result<MyBlogStatusCountsDto>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<MyBlogStatusCountsDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
