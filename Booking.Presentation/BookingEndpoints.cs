using Booking.Presentation.Endpoints.Admin;
using Booking.Presentation.Endpoints.AvailabilitySlot;
using Booking.Presentation.Endpoints.GuideDiscount;
using Booking.Presentation.Endpoints.JoinRequest;
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

        var adminBookingGroup = endpoints.MapGroup("/api/v1/admin/bookings");
        AdminBookingEndpoints.MapAdminBookingEndpoints(adminBookingGroup);

        var slotsGroup = endpoints.MapGroup("/api/v1/booking/slots");
        AvailabilitySlotEndpoints.MapAvailabilitySlotEndpoints(slotsGroup);

        var joinRequestsGroup = endpoints.MapGroup("/api/v1/booking/join-requests");
        JoinRequestEndpoints.MapJoinRequestEndpoints(joinRequestsGroup);

        var guideDiscountsGroup = endpoints.MapGroup("/api/v1/booking/guide-discounts");
        GuideDiscountEndpoints.MapGuideDiscountEndpoints(guideDiscountsGroup);

        return endpoints;
    }
}
