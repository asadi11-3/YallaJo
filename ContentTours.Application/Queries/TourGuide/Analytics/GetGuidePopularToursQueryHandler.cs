using Booking.Contracts.Services;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuide.Analytics;

internal sealed class GetGuidePopularToursQueryHandler(
    IGuideBookingAnalyticsReader reader,
    ILogger<GetGuidePopularToursQueryHandler> logger) : IQueryHandler<GetGuidePopularToursQuery, IReadOnlyList<PopularTour>>
{
    public async Task<Result<IReadOnlyList<PopularTour>>> Handle(GetGuidePopularToursQuery request, CancellationToken cancellationToken)
    {
        var rows = await reader.GetPopularToursAsync(request.GuideUserId, request.Limit, cancellationToken);
        logger.LogInformation("Returned {Count} popular tour rows for GuideUserId={GuideUserId}", rows.Count, request.GuideUserId);
        return Result.Success(rows);
    }
}
