using Booking.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetProviderBookings;

/// <summary>
/// Provider-scoped: list bookings for tours owned by the caller's provider, with
/// cursor pagination and optional filters. Ownership is resolved in the handler
/// (caller's ProviderSnapshot by OwnerUserId); not cached so providers see
/// mutations (confirm/cancel) immediately.
/// </summary>
public sealed record GetProviderBookingsQuery(
    Guid CallerUserId,
    IReadOnlyList<BookingStatus>? Statuses,
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? TourId,
    string? Cursor,
    int PageSize,
    bool CountTotal)
    : IQuery<ProviderBookingsPage>
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
}
