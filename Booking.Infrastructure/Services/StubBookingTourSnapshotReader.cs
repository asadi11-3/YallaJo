using Booking.Application.Interfaces;

namespace Booking.Infrastructure.Services;

/// <summary>
/// STUB implementation of <see cref="IBookingTourSnapshotReader"/>.
/// </summary>
/// <remarks>
/// TODO: Replace with a real reader once the ContentTours team ships the
/// <c>booking.TourSnapshots</c> table populated via the
/// <c>content-tours.tour.published.v1</c> inbox handler.
///
/// Until then, this stub returns a plausible, well-formed snapshot for ANY
/// non-empty tour id. This unblocks the booking-creation handler and the
/// happy-path integration test, while preserving the contract for the eventual
/// real reader.
/// </remarks>
internal sealed class StubBookingTourSnapshotReader : IBookingTourSnapshotReader
{
    public Task<BookingTourSnapshot?> GetByIdAsync(Guid tourId, CancellationToken cancellationToken = default)
    {
        if (tourId == Guid.Empty)
        {
            return Task.FromResult<BookingTourSnapshot?>(null);
        }

        // Deterministic fake provider id derived from tour id so the same tour
        // always maps to the same provider snapshot.
        var providerId = DeriveProviderId(tourId);

        var snapshot = new BookingTourSnapshot(
            TourId: tourId,
            ProviderId: providerId,
            Title: "Stub Tour (replace with real snapshot)",
            Currency: "JOD",
            BasePrice: 30m,
            IsActive: true,
            IsApproved: true,
            IsInstantBooking: false,
            RefundPolicyId: null,
            RefundPolicySnapshotJson: "{\"tiers\":[{\"hoursBeforeTour\":72,\"refundPercent\":100},{\"hoursBeforeTour\":24,\"refundPercent\":50},{\"hoursBeforeTour\":0,\"refundPercent\":0}]}");

        return Task.FromResult<BookingTourSnapshot?>(snapshot);
    }

    private static Guid DeriveProviderId(Guid tourId)
    {
        // XOR the tour id bytes with a fixed mask — purely deterministic so
        // tests are reproducible. Replace when real snapshots ship.
        var bytes = tourId.ToByteArray();
        var mask = new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x23, 0x45, 0x67, 0x89, 0x98, 0x76, 0x54, 0x32, 0x10, 0xFE, 0xDC, 0xBA };
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] ^= mask[i];
        }

        return new Guid(bytes);
    }
}
