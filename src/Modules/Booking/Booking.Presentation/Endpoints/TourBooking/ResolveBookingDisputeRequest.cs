using Booking.Application.Commands.ResolveBookingDispute;

namespace Booking.Presentation.Endpoints.TourBooking;

/// <summary>
/// Body for POST /api/v1/booking/admin/{id}/dispute/resolve. Admin-only.
/// ResolutionNotes 10-2000 chars; booking must be in Disputed status.
/// </summary>
public sealed record ResolveBookingDisputeRequest(string ResolutionNotes)
{
    public ResolveBookingDisputeCommand ToCommand(Guid bookingId) => new(bookingId, ResolutionNotes);
}
