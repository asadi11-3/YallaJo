using ContentTours.Application.Commands.TourWaypoints.AddTourWaypoint;
using ContentTours.Application.Commands.TourWaypoints.RemoveTourWaypoint;
using ContentTours.Application.Commands.TourWaypoints.ReorderTourWaypoints;
using ContentTours.Application.Queries.TourWaypoints.GetByTourId;
using ContentTours.Contracts.Authorization;
using ContentTours.Presentation.Endpoints.TourWaypoint.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.TourWaypoint;

internal static class TourWaypointEndpoints
{
    internal static void MapTourWaypointEndpoints(RouteGroupBuilder group)
    {
        var waypoints = group.MapGroup("/{id:guid}/waypoints")
            .WithTags("ContentTours | Waypoints");

        waypoints.MapGet("/", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTourWaypointsQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("ListTourWaypoints")
        .WithSummary("List waypoints for a tour ordered by SortOrder")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        waypoints.MapPost("/", async (
            Guid id,
            AddTourWaypointRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new AddTourWaypointCommand(
                id,
                request.Name,
                request.Description,
                request.Latitude,
                request.Longitude,
                request.WaypointType,
                request.DurationMinutes);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AddTourWaypoint")
        .WithSummary("Add a waypoint to a tour")
        .Produces<AddTourWaypointResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourWaypoint, AppAction.Create));

        // PDF Task 4A B2: batch reorder — body provides the full ordered WaypointIds list;
        // handler reassigns SortOrder = 0..N-1. Handler validates set-equality + duplicates.
        waypoints.MapPut("/reorder", async (
            Guid id,
            ReorderTourWaypointsRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new ReorderTourWaypointsCommand(id, request.WaypointIds);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("ReorderTourWaypoints")
        .WithSummary("Reorder all waypoints in a tour (batch — PDF B2 shape)")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourWaypoint, AppAction.Update));

        waypoints.MapDelete("/{waypointId:guid}", async (
            Guid id,
            Guid waypointId,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RemoveTourWaypointCommand(id, waypointId);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("RemoveTourWaypoint")
        .WithSummary("Remove a waypoint from a tour (re-compacts SortOrder)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourWaypoint, AppAction.Delete));
    }
}
