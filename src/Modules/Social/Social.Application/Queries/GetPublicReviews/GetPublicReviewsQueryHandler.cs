using ContentCore.Contracts.Attachments;
using Microsoft.Extensions.Logging;
using Social.Application.Queries.Dtos;
using Social.Application.Queries.GetMyReviews;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.GetPublicReviews;

internal sealed class GetPublicReviewsQueryHandler(
    IReviewRepository reviewRepository,
    IPublicEntityImageReader imageReader,
    ILogger<GetPublicReviewsQueryHandler> logger)
    : IQueryHandler<GetPublicReviewsQuery, PublicReviewPageDto>
{
    // Attachments for reviews are stored under ContentCore EntityType "Review"
    // (distinct from the review's TargetType, e.g. Tour/Place).
    private const string ReviewAttachmentEntityType = "Review";

    public async Task<Result<PublicReviewPageDto>> Handle(GetPublicReviewsQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var (items, totalCount) = await reviewRepository.GetPublicListAsync(
            query.EntityType, query.EntityId, page, pageSize, ct);

        // GetPublicListAsync only returns Published reviews, so image URLs are
        // emitted exclusively for published reviews. Batch-load to avoid N+1.
        var dtos = items.Select(GetMyReviewsQueryHandler.MapToDto).ToList();
        var reviewIds = dtos.Select(d => d.Id).ToList();
        var imagesByReview = await imageReader
            .GetEntityImagesBatchAsync(ReviewAttachmentEntityType, reviewIds, ct)
            .ConfigureAwait(false);

        var withImages = dtos
            .Select(dto => dto with
            {
                ImageUrls = imagesByReview.TryGetValue(dto.Id, out var images)
                    ? images.Select(img => img.Url).ToList()
                    : Array.Empty<string>()
            })
            .ToList();

        logger.LogInformation(
            "Loaded {Count} public reviews for {EntityType}/{EntityId}",
            withImages.Count, query.EntityType, query.EntityId);

        return Result.Success(new PublicReviewPageDto(
            withImages, page, pageSize, totalCount));
    }
}
