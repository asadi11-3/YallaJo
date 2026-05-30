using Booking.Contracts.Services;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuide.Analytics;

internal sealed class GetGuideBookingOverviewQueryHandler(
    IGuideBookingAnalyticsReader reader,
    ILogger<GetGuideBookingOverviewQueryHandler> logger) : IQueryHandler<GetGuideBookingOverviewQuery, GuideBookingOverview>
{
    public async Task<Result<GuideBookingOverview>> Handle(GetGuideBookingOverviewQuery request, CancellationToken cancellationToken)
    {
        var overview = await reader.GetOverviewAsync(request.GuideUserId, cancellationToken);
        logger.LogInformation("Returned booking overview for GuideUserId={GuideUserId}", request.GuideUserId);
        return Result.Success(overview);
    }
}
