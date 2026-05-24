using System.Globalization;

namespace Booking.Application.Caching;

public static class BookingRefundPolicyCacheKeys
{
    public static string TourTag(Guid tourId)
        => string.Create(CultureInfo.InvariantCulture, $"refund-policy:tour:{tourId:D}");

    public static string TourLookupKey(Guid tourId)
        => string.Create(CultureInfo.InvariantCulture, $"refund-policy:tour:{tourId:D}:lookup");
}
