namespace Booking.Application.Interfaces;

/// <summary>
/// Per-tour commission rate lookup used by the booking engine when stamping
/// <c>CommissionRate</c>/<c>CommissionAmount</c> onto a TourBooking aggregate.
/// </summary>
/// <remarks>
/// TODO: Wire to Finance.Contracts.Services.ICommissionLookupService once that module's
/// real implementation ships (currently Finance returns a 0.10 stub). Keeping a Booking-local
/// interface preserves the modular monolith boundary and avoids a cross-module reference
/// in Booking.Application during the engine bootstrap sprint.
/// </remarks>
public sealed record BookingCommissionLookupResult(decimal Rate);

public interface IBookingCommissionLookup
{
    Task<BookingCommissionLookupResult> GetForTourAsync(Guid tourId, CancellationToken cancellationToken = default);
}
