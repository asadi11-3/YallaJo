using Booking.Application.Commands.OpenBookingDispute;

namespace Booking.Presentation.Endpoints.TourBooking;

/// <summary>
/// Body for POST /api/v1/booking/{id}/dispute. Owner-only.
/// Reason 10-2000 chars; booking must be Completed within the past 48 hours.
/// </summary>
public sealed record OpenBookingDisputeRequest(string Reason)
{
    public OpenBookingDisputeCommand ToCommand(Guid bookingId) => new(bookingId, Reason);
}
