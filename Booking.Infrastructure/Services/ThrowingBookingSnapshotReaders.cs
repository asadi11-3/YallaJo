using Booking.Application.Interfaces;
using Booking.Domain.Enums;

namespace Booking.Infrastructure.Services;

internal static class ThrowingBookingSnapshotReaders
{
    public const string FailureMessage =
        "Real Booking snapshot reader is not wired. "
        + "Set Booking:AllowStubSnapshotReaders = true in configuration to use the development stubs, "
        + "or register the real inbox-backed reader before calling any Booking handler that depends on it.";
}

internal sealed class ThrowingBookingTourSnapshotReader : IBookingTourSnapshotReader
{
    public Task<BookingTourSnapshot?> GetByIdAsync(Guid tourId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(ThrowingBookingSnapshotReaders.FailureMessage);
}

internal sealed class ThrowingBookingProviderSnapshotReader : IBookingProviderSnapshotReader
{
    public Task<BookingProviderSnapshot?> GetByIdAsync(Guid providerId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(ThrowingBookingSnapshotReaders.FailureMessage);
}

internal sealed class ThrowingBookingPricingSnapshotReader : IBookingPricingSnapshotReader
{
    public Task<BookingPricingTierSnapshot?> GetByTourAndTypeAsync(
        Guid tourId,
        TierType tierType,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(ThrowingBookingSnapshotReaders.FailureMessage);

    public Task<IReadOnlyList<BookingPricingTierSnapshot>> GetAllForTourAsync(
        Guid tourId,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(ThrowingBookingSnapshotReaders.FailureMessage);
}
