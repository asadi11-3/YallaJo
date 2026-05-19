namespace Booking.Application.Interfaces;

/// <summary>
/// Read-only projection of a Provider required by the booking flow.
/// Sourced from a Booking-owned snapshot table populated via inbox handlers
/// listening to providers/business integration events.
/// </summary>
public sealed record BookingProviderSnapshot(
    Guid ProviderId,
    Guid OwnerUserId,
    string DisplayName,
    BookingProviderStatus Status);

public enum BookingProviderStatus
{
    Active = 0,
    Suspended = 1,
    PendingApproval = 2,
    Disabled = 3,
}

/// <summary>
/// Reads cached provider snapshots needed during booking creation.
/// </summary>
public interface IBookingProviderSnapshotReader
{
    Task<BookingProviderSnapshot?> GetByIdAsync(Guid providerId, CancellationToken cancellationToken = default);
}
