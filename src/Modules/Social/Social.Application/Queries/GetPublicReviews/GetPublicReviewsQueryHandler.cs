using Microsoft.Extensions.Logging;
using Social.Application.Queries.Dtos;
using Social.Application.Queries.GetMyReviews;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetPublicReviews;

internal sealed class GetPublicReviewsQueryHandler(
    IReviewRepository reviewRepository,
    ILogger<GetPublicReviewsQueryHandler> logger)
    : IQueryHandler<GetPublicReviewsQuery, PublicReviewPageDto>
{
    public async Task<Result<PublicReviewPageDto>> Handle(GetPublicReviewsQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var (items, totalCount) = await reviewRepository.GetPublicListAsync(
            query.EntityType, query.EntityId, page, pageSize, ct);

        logger.LogInformation(
            "Loaded {Count} public reviews for {EntityType}/{EntityId}",
            items.Count, query.EntityType, query.EntityId);

        return Result.Success(new PublicReviewPageDto(
            items.Select(GetMyReviewsQueryHandler.MapToDto).ToList(), page, pageSize, totalCount));
    }
}
