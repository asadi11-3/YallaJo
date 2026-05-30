using Booking.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetAvailabilityForTour;

public sealed record GetAvailabilityForTourQuery(
    Guid TourId,
    string? Cursor,
    int PageSize,
    bool CountTotal) : IQuery<AvailabilityPage>, ICacheableQuery
{
    public const int DefaultPageSize = 20;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 50;

    public int EffectivePageSize => PageSize <= 0 ? DefaultPageSize : Math.Clamp(PageSize, MinPageSize, MaxPageSize);

    public string CacheKey => BookingAvailabilityCacheKeys.TourListKey(TourId, Cursor, EffectivePageSize, CountTotal);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => [BookingAvailabilityCacheKeys.TourTag(TourId)];
}
