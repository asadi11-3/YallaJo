namespace Booking.Application.Interfaces;

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
    string? RefundPolicySnapshotJson,
    int MaxGroupSize);

public interface IBookingTourSnapshotReader
{
    Task<BookingTourSnapshot?> GetByIdAsync(Guid tourId, CancellationToken cancellationToken = default);
}
