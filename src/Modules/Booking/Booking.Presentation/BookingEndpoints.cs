using Booking.Presentation.Endpoints.Admin;
using Booking.Presentation.Endpoints.AvailabilitySlot;
using Booking.Presentation.Endpoints.GuideDiscount;
using Booking.Presentation.Endpoints.JoinRequest;
using Booking.Presentation.Endpoints.ProviderDocument;
using Booking.Presentation.Endpoints.TourBooking;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Booking.Presentation;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var bookingGroup = endpoints.MapGroup("/api/v1/booking");
        TourBookingEndpoints.MapTourBookingEndpoints(bookingGroup.MapGroup("").WithTags("Booking | Tour Bookings"));
        AvailabilitySlotEndpoints.MapAvailabilitySlotEndpoints(bookingGroup.MapGroup("").WithTags("Booking | Availability Slots"));
        ProviderDocumentEndpoints.MapProviderDocumentEndpoints(bookingGroup.MapGroup("").WithTags("Booking | Provider Documents"));

        var adminBookingGroup = endpoints.MapGroup("/api/v1/admin/bookings");
        AdminBookingEndpoints.MapAdminBookingEndpoints(adminBookingGroup.MapGroup("").WithTags("Booking | Admin"));

        // NOTE: AvailabilitySlotEndpoints is already mounted on `bookingGroup` above
        // (lines 19-21). It used to be mounted a second time on `/api/v1/booking/slots`,
        // which produced duplicate routes like `POST /api/v1/booking/slots/availability/slots`
        // and caused `InvalidOperationException: Duplicate endpoint name 'CreateAvailabilitySlot' ...`
        // at startup. The second mount was a known issue (see Agents/Tests/Playwright-Booking.md TC-BK-034).
        var joinRequestsGroup = endpoints.MapGroup("/api/v1/booking/join-requests");
        JoinRequestEndpoints.MapJoinRequestEndpoints(joinRequestsGroup.MapGroup("").WithTags("Booking | Join Requests"));

        var guideDiscountsGroup = endpoints.MapGroup("/api/v1/booking/guide-discounts");
        GuideDiscountEndpoints.MapGuideDiscountEndpoints(guideDiscountsGroup.MapGroup("").WithTags("Booking | Guide Discounts"));

        return endpoints;
    }
}
