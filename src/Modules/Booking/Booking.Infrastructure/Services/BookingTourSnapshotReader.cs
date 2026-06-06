using Booking.Application.Interfaces;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Services;

/// <summary>
/// Real implementation of IBookingTourSnapshotReader backed by the Booking-owned
/// TourSnapshots table, populated via inbox handlers from ContentTours events.
/// Replaces StubBookingTourSnapshotReader.
/// </summary>
internal sealed class BookingTourSnapshotReader(BookingDbContext dbContext)
    : IBookingTourSnapshotReader
{
    public async Task<BookingTourSnapshot?> GetByIdAsync(
        Guid tourId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await dbContext.TourSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TourId == tourId, cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return null;
        }

        return new BookingTourSnapshot(
            TourId: snapshot.TourId,
            ProviderId: snapshot.ProviderId,
            Title: snapshot.Title,
            Currency: snapshot.Currency,
            BasePrice: snapshot.BasePrice,
            IsActive: snapshot.IsActive,
            IsApproved: snapshot.IsApproved,
            IsInstantBooking: snapshot.IsInstantBooking,
            RefundPolicyId: snapshot.RefundPolicyId,
            RefundPolicySnapshotJson: snapshot.RefundPolicySnapshotJson,
            MaxGroupSize: snapshot.MaxGroupSize);
    }
}
