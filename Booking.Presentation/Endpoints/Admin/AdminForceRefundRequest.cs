using Booking.Application.Commands.AdminForceRefund;

namespace Booking.Presentation.Endpoints.Admin;

public sealed record AdminForceRefundRequest(string Reason)
{
    public AdminForceRefundCommand ToCommand(Guid bookingId) => new(bookingId, Reason);
}
