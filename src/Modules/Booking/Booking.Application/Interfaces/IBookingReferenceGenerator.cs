using Booking.Domain.ValueObjects;

namespace Booking.Application.Interfaces;

/// <summary>
/// Generates unique, collision-resistant booking references in the format YJ-YYYYMMDD-XXXXXX.
/// The implementation must use a cryptographic RNG and retry on rare uniqueness collisions.
/// </summary>
public interface IBookingReferenceGenerator
{
    Task<BookingReference> GenerateAsync(CancellationToken cancellationToken = default);
}
