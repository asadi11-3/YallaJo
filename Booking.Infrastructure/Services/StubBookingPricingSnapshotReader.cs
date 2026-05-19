using Booking.Application.Interfaces;
using Booking.Domain.Enums;

namespace Booking.Infrastructure.Services;

/// <summary>
/// STUB implementation of <see cref="IBookingPricingSnapshotReader"/>.
/// </summary>
/// <remarks>
/// TODO: Replace with a real reader once the ContentTours team ships the
/// <c>booking.PricingTierSnapshots</c> table populated via the
/// <c>content-tours.tour-pricing-tier.published.v1</c> inbox handler.
///
/// Until then, this stub returns <c>null</c> for every lookup so the
/// booking-creation handler falls back to the tour snapshot's <c>BasePrice</c>
/// for all participant tiers. This intentionally exercises the "no tier
/// configured" pricing path, which is the most common dev/test scenario.
/// </remarks>
internal sealed class StubBookingPricingSnapshotReader : IBookingPricingSnapshotReader
{
    private static readonly IReadOnlyList<BookingPricingTierSnapshot> Empty = [];

    public Task<BookingPricingTierSnapshot?> GetByTourAndTypeAsync(
        Guid tourId,
        TierType tierType,
        CancellationToken cancellationToken = default)
        => Task.FromResult<BookingPricingTierSnapshot?>(null);

    public Task<IReadOnlyList<BookingPricingTierSnapshot>> GetAllForTourAsync(
        Guid tourId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Empty);
}
