using Booking.Application.Commands.Common;
using Booking.Application.Commands.CreateAvailabilitySlot;
using Booking.Application.Commands.CreateBulkAvailabilitySlots;
using Booking.Application.Commands.DeleteAvailabilitySlot;
using Booking.Application.Commands.UpdateAvailabilitySlot;
using Booking.Application.Queries.GetAvailabilityForTour;
using Booking.Application.Queries.GetAvailabilityForTourOnDate;
using Booking.Application.Queries.GetManageAvailabilityForTour;
using Booking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Booking.Presentation.Endpoints.AvailabilitySlot;

internal static class AvailabilitySlotEndpoints
{
    internal static void MapAvailabilitySlotEndpoints(RouteGroupBuilder group)
    {
        MapCreateEndpoint(group);
        MapCreateBulkEndpoint(group);
        MapUpdateEndpoint(group);
        MapDeleteEndpoint(group);
        MapGetByTourEndpoint(group);
        MapGetByTourOnDateEndpoint(group);
        MapGetManageByTourEndpoint(group);
    }

    private static void MapGetManageByTourEndpoint(RouteGroupBuilder group)
    {
        // Owner-scoped management list: all slots (active + inactive) for a tour the
        // caller provider owns, including RowVersion for edit/delete. Ownership is
        // enforced inside the query handler (provider owner or admin).
        group.MapGet("/availability/{tourId:guid}/manage", async (
                Guid tourId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetManageAvailabilityForTourQuery(tourId),
                    cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetManageAvailabilityForTour")
            .WithSummary("Owner: list all availability slots (incl. inactive) for a tour, with RowVersion.")
            .Produces<IReadOnlyList<ManageAvailabilitySlotDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.AvailabilitySlot, AppAction.Read))
            .RequireAuthorization();
    }

    private static void MapCreateEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/availability/slots", async (
                CreateAvailabilitySlotRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("CreateAvailabilitySlot")
            .WithSummary("Create one availability slot for a tour.")
            .Accepts<CreateAvailabilitySlotRequest>("application/json")
            .Produces<AvailabilitySlotDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.AvailabilitySlot, AppAction.Create))
            .RequireAuthorization();
    }

    private static void MapCreateBulkEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/availability/slots/bulk", async (
                CreateBulkAvailabilitySlotsRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var parsed = request.ToCommand();
                if (!parsed.IsSuccess)
                {
                    return parsed.ToApiResult();
                }
        
                var result = await sender.Send(parsed.Value!, cancellationToken);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToApiResult();
            })
            .WithName("CreateBulkAvailabilitySlots")
            .WithSummary("Create recurring availability slots for a tour.")
            .Accepts<CreateBulkAvailabilitySlotsRequest>("application/json")
            .Produces<CreateBulkAvailabilitySlotsResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.AvailabilitySlot, AppAction.Create))
            .RequireAuthorization();
    }

    private static void MapUpdateEndpoint(RouteGroupBuilder group)
    {
        group.MapPut("/availability/slots/{id:guid}", async (
                Guid id,
                UpdateAvailabilitySlotRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("UpdateAvailabilitySlot")
            .WithSummary("Update availability slot capacity.")
            .Accepts<UpdateAvailabilitySlotRequest>("application/json")
            .Produces<AvailabilitySlotDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.AvailabilitySlot, AppAction.Update))
            .RequireAuthorization();
    }

    private static void MapDeleteEndpoint(RouteGroupBuilder group)
    {
        group.MapDelete("/availability/slots/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken,
                string? rowVersion = null) =>
            {
                byte[] decoded;
                try
                {
                    decoded = string.IsNullOrWhiteSpace(rowVersion)
                        ? []
                        : Convert.FromBase64String(rowVersion);
                }
                catch (FormatException)
                {
                    decoded = [];
                }
        
                var result = await sender.Send(new DeleteAvailabilitySlotCommand(id, decoded), cancellationToken);
                return result.IsSuccess
                    ? Results.NoContent()
                    : result.ToApiResult();
            })
            .WithName("DeleteAvailabilitySlot")
            .WithSummary("Deactivate an availability slot.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.AvailabilitySlot, AppAction.Delete))
            .RequireAuthorization();
    }

    private static void MapGetByTourEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/availability/{tourId:guid}", async (
                Guid tourId,
                ISender sender,
                CancellationToken cancellationToken,
                string? cursor = null,
                int pageSize = GetAvailabilityForTourQuery.DefaultPageSize,
                bool countTotal = false) =>
            {
                var result = await sender.Send(
                    new GetAvailabilityForTourQuery(tourId, cursor, pageSize, countTotal),
                    cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetAvailabilityForTour")
            .WithSummary("Get cursor-paginated availability grouped by date for a tour.")
            .Produces<AvailabilityPage>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();
    }

    private static void MapGetByTourOnDateEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/availability/{tourId:guid}/{date}", async (
                Guid tourId,
                DateOnly date,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetAvailabilityForTourOnDateQuery(tourId, date),
                    cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetAvailabilityForTourOnDate")
            .WithSummary("Get availability slots for a tour on a specific date.")
            .Produces<AvailabilityForDateDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();
    }
}
