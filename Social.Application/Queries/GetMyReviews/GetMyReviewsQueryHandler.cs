using MediatR;
using Social.Application.Queries.Dtos;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetMyReviews;

internal sealed class GetMyReviewsQueryHandler(IReviewRepository reviewRepository)
    : IRequestHandler<GetMyReviewsQuery, Result<ReviewPageDto>>
{
    public async Task<Result<ReviewPageDto>> Handle(GetMyReviewsQuery query, CancellationToken ct)
    {
        var size = Math.Clamp(query.PageSize, 1, 50);
        var (items, nextCursor) = await reviewRepository.GetByUserPageAsync(
            query.UserId, query.AfterCursor, size, ct);

        var dtos = items.Select(MapToDto).ToList();
        var nextToken = nextCursor.HasValue
            ? nextCursor.Value.ToString("N")
            : null;

        return Result.Success(new ReviewPageDto(dtos, nextToken));
    }

    internal static ReviewDto MapToDto(Social.Domain.Entities.Review r) => new(
        r.Id,
        r.UserId,
        r.TargetType.ToString(),
        r.TargetId,
        r.Rating,
        r.Title,
        r.Content,
        r.VisitDate,
        r.Status.ToString(),
        r.IsVerifiedBooking,
        r.ProfanityFlagged,
        r.CurrentReportCount,
        r.LastEditedAt,
        r.AutoHiddenAt,
        r.CreatedAt,
        r.Replies
            .Where(rp => !rp.IsDeleted)
            .Select(rp => new ReviewReplyDto(
                rp.Id,
                rp.ReviewId,
                rp.ProviderUserId,
                rp.Content,
                rp.CreatedAt,
                rp.LastEditedAt))
            .ToList()
    );
}
