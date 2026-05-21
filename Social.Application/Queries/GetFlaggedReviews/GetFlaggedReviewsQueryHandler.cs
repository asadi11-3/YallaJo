using MediatR;
using Social.Application.Queries.Dtos;
using Social.Application.Queries.GetMyReviews;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetFlaggedReviews;

internal sealed class GetFlaggedReviewsQueryHandler(IReviewRepository reviewRepository)
    : IRequestHandler<GetFlaggedReviewsQuery, Result<ReviewPageDto>>
{
    public async Task<Result<ReviewPageDto>> Handle(GetFlaggedReviewsQuery query, CancellationToken ct)
    {
        var size = Math.Clamp(query.PageSize, 1, 50);
        var (items, nextCursor) = await reviewRepository.GetFlaggedPageAsync(
            query.AfterCursor, size, ct);

        var dtos = items.Select(GetMyReviewsQueryHandler.MapToDto).ToList();
        var nextToken = nextCursor.HasValue
            ? nextCursor.Value.ToString("N")
            : null;

        return Result.Success(new ReviewPageDto(dtos, nextToken));
    }
}
