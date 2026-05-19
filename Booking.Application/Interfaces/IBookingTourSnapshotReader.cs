namespace Booking.Application.Interfaces;

/// <summary>
/// Read-only projection of a Tour required by the booking flow.
/// Sourced from a Booking-owned snapshot table populated via inbox handlers
/// listening to content.tours integration events.
/// </summary>
public sealed record BookingTourSnapshot(
    Guid TourId,
    Guid ProviderId,
    string Title,
    string Currency,
    decimal BasePrice,
    bool IsActive,
    bool IsApproved,
    bool IsInstantBooking,
    Guid? RefundPolicyId,
    string? RefundPolicySnapshotJson);

/// <summary>
/// Reads cached tour snapshots needed during booking creation.
/// </summary>
public interface IBookingTourSnapshotReader
{
    Task<BookingTourSnapshot?> GetByIdAsync(Guid tourId, CancellationToken cancellationToken = default);
}
