using Finance.Contracts.Services;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuide.Earnings;

internal sealed class GetGuideEarningsSummaryQueryHandler(
    IGuideEarningReader reader,
    ILogger<GetGuideEarningsSummaryQueryHandler> logger) : IQueryHandler<GetGuideEarningsSummaryQuery, GuideEarningsSummary>
{
    public async Task<Result<GuideEarningsSummary>> Handle(GetGuideEarningsSummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await reader.GetSummaryAsync(request.GuideUserId, cancellationToken);
        logger.LogInformation("Returned earnings summary for GuideUserId={GuideUserId}", request.GuideUserId);
        return Result.Success(summary);
    }
}
