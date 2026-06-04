using Booking.Domain.Enums;

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

/// <summary>
/// Reads cached provider snapshots needed during booking creation.
/// </summary>
public interface IBookingProviderSnapshotReader
{
    Task<BookingProviderSnapshot?> GetByIdAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the provider snapshot owned by the given user (reverse of
    /// <see cref="BookingProviderSnapshot.OwnerUserId"/>). Returns <c>null</c> when the
    /// user owns no provider. Used to scope provider-facing booking queries to the caller.
    /// </summary>
    Task<BookingProviderSnapshot?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
}
