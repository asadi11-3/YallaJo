using Booking.Application.Commands.CancelTourBooking;

namespace Booking.Presentation.Endpoints.TourBooking;

/// <summary>
/// Body for POST /api/v1/booking/{id}/cancel. Reason is optional for the booking owner,
/// mandatory (>=10 chars) for provider or admin cancellations (handler enforces).
/// </summary>
public sealed record CancelTourBookingRequest(string? Reason)
{
    public CancelTourBookingCommand ToCommand(Guid bookingId) => new(bookingId, Reason);
}
