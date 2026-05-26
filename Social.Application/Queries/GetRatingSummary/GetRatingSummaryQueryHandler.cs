using Microsoft.Extensions.Logging;
using Social.Application.Queries.Dtos;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetRatingSummary;

internal sealed class GetRatingSummaryQueryHandler(
    IReviewRepository reviewRepository,
    ILogger<GetRatingSummaryQueryHandler> logger)
    : IQueryHandler<GetRatingSummaryQuery, RatingSummaryDto>
{
    public async Task<Result<RatingSummaryDto>> Handle(GetRatingSummaryQuery query, CancellationToken ct)
    {
        var reviews = await reviewRepository.GetPublishedByTargetAsync(query.EntityType, query.EntityId, ct);
        var average = reviews.Count == 0 ? 0m : decimal.Round(reviews.Average(r => r.Rating), 1);
        logger.LogInformation("Loaded rating summary for {EntityType}/{EntityId}: {Count} reviews", query.EntityType, query.EntityId, reviews.Count);
        return Result.Success(new RatingSummaryDto(query.EntityType.ToString(), query.EntityId, average, reviews.Count));
    }
}
