using Booking.Domain.Enums;

namespace Booking.Application.Interfaces;

/// <summary>
/// A per-tier price snapshot for a tour.
/// </summary>
public sealed record BookingPricingTierSnapshot(
    Guid TourId,
    TierType TierType,
    decimal Price,
    string Currency);

/// <summary>
/// Reads per-tier pricing tables for a tour.
/// Sourced from a Booking-owned snapshot table populated via inbox handlers.
/// </summary>
public interface IBookingPricingSnapshotReader
{
    Task<BookingPricingTierSnapshot?> GetByTourAndTypeAsync(
        Guid tourId,
        TierType tierType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingPricingTierSnapshot>> GetAllForTourAsync(
        Guid tourId,
        CancellationToken cancellationToken = default);
}
