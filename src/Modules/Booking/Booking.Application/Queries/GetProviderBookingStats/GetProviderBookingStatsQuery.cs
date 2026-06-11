using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetProviderBookingStats;

/// <summary>
/// [Backend] B5: aggregate per-status booking counts for the calling provider,
/// optionally scoped to a slot-date range (same date semantics as the
/// provider bookings list). Additive — does not touch the list endpoint.
/// </summary>
public sealed record GetProviderBookingStatsQuery(
    Guid CallerUserId,
    DateOnly? FromDate,
    DateOnly? ToDate) : IQuery<ProviderBookingStatsDto>;
