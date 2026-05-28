using Booking.Contracts.Services;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuide.Analytics;

internal sealed class GetGuideBookingTrendsQueryHandler(
    IGuideBookingAnalyticsReader reader,
    ILogger<GetGuideBookingTrendsQueryHandler> logger) : IQueryHandler<GetGuideBookingTrendsQuery, IReadOnlyList<BookingTrend>>
{
    public async Task<Result<IReadOnlyList<BookingTrend>>> Handle(GetGuideBookingTrendsQuery request, CancellationToken cancellationToken)
    {
        var rows = await reader.GetBookingTrendsAsync(request.GuideUserId, request.Granularity, request.Months, cancellationToken);
        logger.LogInformation("Returned {Count} booking trend rows for GuideUserId={GuideUserId}", rows.Count, request.GuideUserId);
        return Result.Success(rows);
    }
}
