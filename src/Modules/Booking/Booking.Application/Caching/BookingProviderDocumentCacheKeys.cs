using System.Globalization;

namespace Booking.Application.Caching;

public static class BookingProviderDocumentCacheKeys
{
    public static string ProviderTag(Guid tourGuideId)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"provider-documents:{tourGuideId:D}");

    public static string UserTag(Guid userId)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"provider-documents:user:{userId:D}");

    public static string DocumentTag(Guid documentId)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"provider-document:{documentId:D}");

    public static string UserListKey(Guid userId)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"provider-documents:user:{userId:D}:list");

    public static string ByIdKey(Guid documentId)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"provider-document:{documentId:D}");
}
