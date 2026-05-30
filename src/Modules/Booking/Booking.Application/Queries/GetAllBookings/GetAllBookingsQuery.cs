using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Booking.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetAllBookings;

/// <summary>
/// Admin: list bookings across all users + providers with cursor pagination and filters.
/// Cache TTL is intentionally short (30s) per spec because admin dashboards are
/// highly volatile and must reflect mutations near-real-time.
/// </summary>
public sealed record GetAllBookingsQuery(
    IReadOnlyList<BookingStatus>? Statuses,
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? UserId,
    Guid? ProviderId,
    Guid? TourId,
    string? PaymentStatus,
    string? Cursor,
    int PageSize,
    bool CountTotal)
    : IQuery<AdminBookingsPage>, ICacheableQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
    public const int MinPageSize = 1;

    public int EffectivePageSize => PageSize switch
    {
        <= 0 => DefaultPageSize,
        var n when n > MaxPageSize => MaxPageSize,
        var n when n < MinPageSize => MinPageSize,
        var n => n,
    };

    public string CacheKey => $"bookings:admin:list:{ComputeFingerprint()}";

    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);

    /// <summary>
    /// Coarse <c>bookings:admin</c> tag is intentional: any booking mutation
    /// across the system must invalidate every admin page (spec B-R10).
    /// </summary>
    public IReadOnlyList<string> Tags => ["bookings:admin"];

    private string ComputeFingerprint()
    {
        var builder = new StringBuilder(256);
        builder.Append("v1|");
        builder.Append(EffectivePageSize).Append('|');
        builder.Append(CountTotal).Append('|');
        builder.Append(Cursor ?? string.Empty).Append('|');

        if (Statuses is { Count: > 0 })
        {
            foreach (var status in Statuses.Distinct().OrderBy(x => x))
            {
                builder.Append((int)status).Append(',');
            }
        }

        builder.Append('|');
        builder.Append(FromDate?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty).Append('|');
        builder.Append(ToDate?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty).Append('|');
        builder.Append(UserId?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty).Append('|');
        builder.Append(ProviderId?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty).Append('|');
        builder.Append(TourId?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty).Append('|');
        builder.Append((PaymentStatus ?? string.Empty).Trim().ToUpperInvariant());

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).AsSpan(0, 16).ToString().ToLowerInvariant();
    }
}
