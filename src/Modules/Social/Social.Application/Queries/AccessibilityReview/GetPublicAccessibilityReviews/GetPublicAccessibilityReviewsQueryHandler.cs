using Microsoft.Extensions.Logging;
using Social.Application.Queries.AccessibilityReview.Common;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.AccessibilityReview.GetPublicAccessibilityReviews;

public sealed class GetPublicAccessibilityReviewsQueryHandler(
    IAccessibilityReviewRepository repository,
    ILogger<GetPublicAccessibilityReviewsQueryHandler> logger)
    : IQueryHandler<GetPublicAccessibilityReviewsQuery, PublicAccessibilityReviewPageDto>
{
    public async Task<Result<PublicAccessibilityReviewPageDto>> Handle(
        GetPublicAccessibilityReviewsQuery request, CancellationToken ct)
    {
        var page     = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var (items, total) = await repository.GetPublicListAsync(
            request.TargetType, request.TargetId, page, pageSize, ct).ConfigureAwait(false);

        var dtos = items.Select(AccessibilityReviewDto.From).ToList();
        logger.LogInformation(
            "Listed {Count} accessibility reviews for {TargetType}/{TargetId} (page {Page} of total {Total})",
            dtos.Count, request.TargetType, request.TargetId, page, total);

        return Result.Success(new PublicAccessibilityReviewPageDto(dtos, page, pageSize, total));
    }
}
