using Booking.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetAvailabilityForTourOnDate;

public sealed record GetAvailabilityForTourOnDateQuery(
    Guid TourId,
    DateOnly Date) : IQuery<AvailabilityForDateDto>, ICacheableQuery
{
    public string CacheKey => BookingAvailabilityCacheKeys.TourOnDateKey(TourId, Date);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
    [
        BookingAvailabilityCacheKeys.TourTag(TourId),
        BookingAvailabilityCacheKeys.TourDateTag(TourId, Date),
    ];
}
