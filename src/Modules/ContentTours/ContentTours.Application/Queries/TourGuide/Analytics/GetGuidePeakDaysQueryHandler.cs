using Booking.Contracts.Services;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuide.Analytics;

internal sealed class GetGuidePeakDaysQueryHandler(
    IGuideBookingAnalyticsReader reader,
    ILogger<GetGuidePeakDaysQueryHandler> logger) : IQueryHandler<GetGuidePeakDaysQuery, IReadOnlyList<PeakDayStat>>
{
    public async Task<Result<IReadOnlyList<PeakDayStat>>> Handle(GetGuidePeakDaysQuery request, CancellationToken cancellationToken)
    {
        var rows = await reader.GetPeakDaysAsync(request.GuideUserId, cancellationToken);
        logger.LogInformation("Returned {Count} peak day rows for GuideUserId={GuideUserId}", rows.Count, request.GuideUserId);
        return Result.Success(rows);
    }
}
