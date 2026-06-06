using Microsoft.Extensions.Logging;
using Social.Application.Queries.AccessibilityReview.Common;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Queries.AccessibilityReview.GetMyAccessibilityReviews;

public sealed class GetMyAccessibilityReviewsQueryHandler(
    IAccessibilityReviewRepository repository,
    ILogger<GetMyAccessibilityReviewsQueryHandler> logger)
    : IQueryHandler<GetMyAccessibilityReviewsQuery, AccessibilityReviewPageDto>
{
    public async Task<Result<AccessibilityReviewPageDto>> Handle(
        GetMyAccessibilityReviewsQuery request, CancellationToken ct)
    {
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;
        var (items, nextCursor) = await repository.GetByUserPageAsync(
            request.UserId, request.AfterId, pageSize, ct).ConfigureAwait(false);

        var dtos = items.Select(AccessibilityReviewDto.From).ToList();
        logger.LogInformation("Listed {Count} accessibility reviews for user {UserId}",
            dtos.Count, request.UserId);
        return Result.Success(new AccessibilityReviewPageDto(dtos, nextCursor));
    }
}
