using System.Globalization;

namespace Booking.Application.Caching;

public static class BookingAvailabilityCacheKeys
{
    public static string TourTag(Guid tourId)
        => $"availability:tour:{tourId:D}";

    public static string TourDateTag(Guid tourId, DateOnly date)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"availability:tour:{tourId:D}:date:{date:yyyy-MM-dd}");

    public static string TourListKey(Guid tourId, string? cursor, int pageSize, bool countTotal)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"availability:tour:{tourId:D}:list:ps={pageSize}:total={(countTotal ? 1 : 0)}:cur={cursor ?? "_"}");

    public static string TourOnDateKey(Guid tourId, DateOnly date)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"availability:tour:{tourId:D}:date:{date:yyyy-MM-dd}:slots");
}
