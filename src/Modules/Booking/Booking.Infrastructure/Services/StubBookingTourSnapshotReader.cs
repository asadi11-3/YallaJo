using Booking.Application.Interfaces;

namespace Booking.Infrastructure.Services;

internal sealed class StubBookingTourSnapshotReader : IBookingTourSnapshotReader
{
    public Task<BookingTourSnapshot?> GetByIdAsync(Guid tourId, CancellationToken cancellationToken = default)
    {
        if (tourId == Guid.Empty)
        {
            return Task.FromResult<BookingTourSnapshot?>(null);
        }

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
            RefundPolicySnapshotJson: "{\"tiers\":[{\"hoursBeforeTour\":72,\"refundPercent\":100},{\"hoursBeforeTour\":24,\"refundPercent\":50},{\"hoursBeforeTour\":0,\"refundPercent\":0}]}",
            MaxGroupSize: 18);

        return Task.FromResult<BookingTourSnapshot?>(snapshot);
    }

    private static Guid DeriveProviderId(Guid tourId)
    {
        var bytes = tourId.ToByteArray();
        var mask = new byte[] { 0xAB, 0xCD, 0xEF, 0x01, 0x23, 0x45, 0x67, 0x89, 0x98, 0x76, 0x54, 0x32, 0x10, 0xFE, 0xDC, 0xBA };
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] ^= mask[i];
        }

        return new Guid(bytes);
    }
}
