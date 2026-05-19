using Booking.Application.Interfaces;

namespace Booking.Infrastructure.Services;

/// <summary>
/// Sprint-bootstrap stub. Returns a flat 10% rate for every tour.
/// </summary>
/// <remarks>
/// TODO: Replace with a Finance-driven lookup (per-provider / per-tour rules) when
/// Finance.Application's CommissionLookupService becomes non-trivial.
/// </remarks>
internal sealed class StubBookingCommissionLookup : IBookingCommissionLookup
{
    private const decimal DefaultRate = 0.10m;

    public Task<BookingCommissionLookupResult> GetForTourAsync(Guid tourId, CancellationToken cancellationToken = default)
        => Task.FromResult(new BookingCommissionLookupResult(DefaultRate));
}
