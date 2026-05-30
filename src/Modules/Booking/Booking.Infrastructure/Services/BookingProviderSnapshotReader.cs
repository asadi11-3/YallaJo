using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Services;

/// <summary>
/// Real implementation of IBookingProviderSnapshotReader backed by the Booking-owned
/// ProviderSnapshots table, populated via inbox handlers from Accounts events.
/// Replaces StubBookingProviderSnapshotReader.
/// </summary>
internal sealed class BookingProviderSnapshotReader(BookingDbContext dbContext)
    : IBookingProviderSnapshotReader
{
    public async Task<BookingProviderSnapshot?> GetByIdAsync(
        Guid providerId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await dbContext.ProviderSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProviderId == providerId, cancellationToken)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return null;
        }

        return new BookingProviderSnapshot(
            ProviderId: snapshot.ProviderId,
            OwnerUserId: snapshot.OwnerUserId,
            DisplayName: snapshot.DisplayName,
            Status: snapshot.Status);
    }
}
