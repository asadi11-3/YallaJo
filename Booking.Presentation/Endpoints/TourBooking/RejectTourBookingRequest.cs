using Booking.Application.Commands.RejectTourBooking;

namespace Booking.Presentation.Endpoints.TourBooking;

/// <summary>
/// Body for POST /api/v1/booking/{id}/reject.
/// Reason is required (10-500 chars).
/// </summary>
public sealed record RejectTourBookingRequest(string Reason)
{
    public RejectTourBookingCommand ToCommand(Guid bookingId) => new(bookingId, Reason);
}
