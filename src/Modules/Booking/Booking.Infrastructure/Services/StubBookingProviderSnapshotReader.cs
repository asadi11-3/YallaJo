using Booking.Application.Interfaces;
using Booking.Domain.Enums;

namespace Booking.Infrastructure.Services;

/// <summary>
/// STUB implementation of <see cref="IBookingProviderSnapshotReader"/>.
/// </summary>
/// <remarks>
/// TODO: Replace with a real reader once the Identity / Provider team ships the
/// <c>booking.ProviderSnapshots</c> table populated via the
/// <c>identity.provider.created.v1</c> / <c>.suspended.v1</c> inbox handlers.
///
/// Until then, this stub returns an Active provider for ANY non-empty provider id
/// so the booking-creation handler can exercise its happy path. Suspended /
/// missing provider tests will need to override this in the test fixture.
/// </remarks>
internal sealed class StubBookingProviderSnapshotReader : IBookingProviderSnapshotReader
{
    public Task<BookingProviderSnapshot?> GetByIdAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        if (providerId == Guid.Empty)
        {
            return Task.FromResult<BookingProviderSnapshot?>(null);
        }

        var snapshot = new BookingProviderSnapshot(
            ProviderId: providerId,
            OwnerUserId: DeriveOwnerUserId(providerId),
            DisplayName: "Stub Provider (replace with real snapshot)",
            Status: BookingProviderStatus.Active);

        return Task.FromResult<BookingProviderSnapshot?>(snapshot);
    }

    private static Guid DeriveOwnerUserId(Guid providerId)
    {
        var bytes = providerId.ToByteArray();
        var mask = new byte[] { 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] ^= mask[i];
        }

        return new Guid(bytes);
    }
}
