using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Booking.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetMyBookings;

/// <summary>
/// Cursor-paginated query for the currently authenticated user's tour bookings.
/// Implements B-R10 (cursor pagination) and the GET /my-bookings spec filters.
/// </summary>
public sealed record GetMyBookingsQuery(
    Guid UserId,
    IReadOnlyList<BookingStatus>? Statuses,
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? TourId,
    string? Cursor,
    int PageSize,
    bool CountTotal) : IQuery<MyBookingsPage>, ICacheableQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
    public const int MinPageSize = 1;

    public string CacheKey => BuildCacheKey();

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(1);

    public IReadOnlyList<string> Tags => [$"bookings:user:{UserId:D}"];

    /// <summary>
    /// Returns the page size clamped to [MinPageSize, MaxPageSize].
    /// </summary>
    public int EffectivePageSize =>
        PageSize <= 0
            ? DefaultPageSize
            : Math.Clamp(PageSize, MinPageSize, MaxPageSize);

    private string BuildCacheKey()
    {
        var fingerprint = ComputeFilterFingerprint();
        return $"bookings:user:{UserId:D}:list:{fingerprint}";
    }

    private string ComputeFilterFingerprint()
    {
        var builder = new StringBuilder(capacity: 128);
        builder.Append("ps=").Append(EffectivePageSize.ToString(CultureInfo.InvariantCulture));
        builder.Append(";total=").Append(CountTotal ? '1' : '0');
        builder.Append(";cur=").Append(Cursor ?? string.Empty);

        if (Statuses is { Count: > 0 })
        {
            builder.Append(";st=");
            var ordered = Statuses
                .Select(static s => (int)s)
                .Distinct()
                .OrderBy(static n => n);
            var first = true;
            foreach (var status in ordered)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                builder.Append(status.ToString(CultureInfo.InvariantCulture));
                first = false;
            }
        }

        if (FromDate.HasValue)
        {
            builder.Append(";from=").Append(FromDate.Value.ToString("o", CultureInfo.InvariantCulture));
        }

        if (ToDate.HasValue)
        {
            builder.Append(";to=").Append(ToDate.Value.ToString("o", CultureInfo.InvariantCulture));
        }

        if (TourId.HasValue)
        {
            builder.Append(";tour=").Append(TourId.Value.ToString("D"));
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash, 0, 8); // 16-char fingerprint is enough.
    }
}
