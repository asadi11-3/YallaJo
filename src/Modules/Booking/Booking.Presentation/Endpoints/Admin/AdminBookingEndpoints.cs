using Booking.Application.Commands.AdminForceRefund;
using Booking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Booking.Presentation.Endpoints.Admin;

/// <summary>
/// Admin-only booking endpoints mapped under <c>/api/v1/admin/bookings</c>.
/// </summary>
internal static class AdminBookingEndpoints
{
    internal static void MapAdminBookingEndpoints(RouteGroupBuilder group)
    {
        MapForceRefundEndpoint(group);
    }

    private static void MapForceRefundEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/force-refund", async (
            Guid id,
            AdminForceRefundRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(request.ToCommand(id), cancellationToken);
            return result.ToApiResult();
        })
        .WithName("AdminForceRefundBooking")
        .WithSummary("Admin: force-majeure cancel + 100% refund a booking (bypasses RefundPolicy).")
        .WithDescription(
            "Used by support for force-majeure scenarios (e.g. natural disaster, provider permanently unavailable). " +
            "Returns 100% of TotalAmount regardless of any RefundPolicy. Audit-logged.")
        .Accepts<AdminForceRefundRequest>("application/json")
        .Produces<AdminForceRefundResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.AdminBookingDashboard, AppAction.Update))
        .RequireAuthorization();
    }
}
