using Booking.Application.Interfaces;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Services;

/// <summary>
/// Real implementation of IBookingPricingSnapshotReader backed by the Booking-owned
/// PricingTierSnapshots table, populated via inbox handlers from ContentTours events.
/// Replaces StubBookingPricingSnapshotReader.
/// </summary>
internal sealed class BookingPricingSnapshotReader(BookingDbContext dbContext)
    : IBookingPricingSnapshotReader
{
    public async Task<BookingPricingTierSnapshot?> GetByTourAndTypeAsync(
        Guid tourId,
        Booking.Domain.Enums.TierType tierType,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await dbContext.PricingTierSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TourId == tourId && p.TierType == tierType, cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return null;
        }

        return new BookingPricingTierSnapshot(
            TourId: snapshot.TourId,
            TierType: snapshot.TierType,
            Price: snapshot.Price,
            Currency: snapshot.Currency);
    }

    public async Task<IReadOnlyList<BookingPricingTierSnapshot>> GetAllForTourAsync(
        Guid tourId,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await dbContext.PricingTierSnapshots
            .AsNoTracking()
            .Where(p => p.TourId == tourId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshots
            .Select(s => new BookingPricingTierSnapshot(
                TourId: s.TourId,
                TierType: s.TierType,
                Price: s.Price,
                Currency: s.Currency))
            .ToList();
    }
}
