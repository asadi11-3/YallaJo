using Booking.Presentation.Endpoints.Admin;
using Booking.Presentation.Endpoints.AvailabilitySlot;
using Booking.Presentation.Endpoints.ProviderDocument;
using Booking.Presentation.Endpoints.TourBooking;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Booking.Presentation;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var bookingGroup = endpoints.MapGroup("/api/v1/booking");
        TourBookingEndpoints.MapTourBookingEndpoints(bookingGroup);
        AvailabilitySlotEndpoints.MapAvailabilitySlotEndpoints(bookingGroup);
        ProviderDocumentEndpoints.MapProviderDocumentEndpoints(bookingGroup);

        var adminBookingGroup = endpoints.MapGroup("/api/v1/admin/bookings");
        AdminBookingEndpoints.MapAdminBookingEndpoints(adminBookingGroup);

        return endpoints;
    }
}
