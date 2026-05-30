using Finance.Contracts.Services;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuide.Earnings;

internal sealed class GetGuideEarningsByTourQueryHandler(
    IGuideEarningReader reader,
    ILogger<GetGuideEarningsByTourQueryHandler> logger) : IQueryHandler<GetGuideEarningsByTourQuery, IReadOnlyList<GuideEarningByTour>>
{
    public async Task<Result<IReadOnlyList<GuideEarningByTour>>> Handle(GetGuideEarningsByTourQuery request, CancellationToken cancellationToken)
    {
        var rows = await reader.GetByTourAsync(request.GuideUserId, cancellationToken);
        logger.LogInformation("Returned {Count} earnings-by-tour rows for GuideUserId={GuideUserId}", rows.Count, request.GuideUserId);
        return Result.Success(rows);
    }
}
