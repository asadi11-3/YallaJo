using Finance.Contracts.Services;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuide.Earnings;

internal sealed class GetGuideEarningsHistoryQueryHandler(
    IGuideEarningReader reader,
    ILogger<GetGuideEarningsHistoryQueryHandler> logger) : IQueryHandler<GetGuideEarningsHistoryQuery, PaginatedResult<GuideEarningHistoryItem>>
{
    public async Task<Result<PaginatedResult<GuideEarningHistoryItem>>> Handle(GetGuideEarningsHistoryQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var history = await reader.GetHistoryAsync(request.GuideUserId, page, pageSize, cancellationToken);
        logger.LogInformation("Returned {Count} earning history rows for GuideUserId={GuideUserId}", history.Items.Count, request.GuideUserId);
        return Result.Success(history);
    }
}
