using ContentTours.Application.Features.TourWaypoints.Commands.Add;
using ContentTours.Application.Features.TourWaypoints.Commands.Remove;
using ContentTours.Application.Features.TourWaypoints.Commands.Reorder;
using ContentTours.Application.Features.TourWaypoints.Queries.GetByTourId;
using ContentTours.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ContentTours.Presentation.Endpoints;

public static class TourWaypointsEndpoints
{
    public static void MapTourWaypointsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tours/{tourId:guid}/waypoints");

        group.MapGet("/", async (Guid tourId, ISender sender, CancellationToken cancellationToken) =>
        {
            var query = new GetTourWaypointsQuery(tourId);
            var result = await sender.Send(query, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/", async (Guid tourId, [FromBody] AddTourWaypointRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new AddTourWaypointCommand(tourId, request.Name, request.Description, request.Latitude, request.Longitude, request.DurationMinutes, request.WaypointType);
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest();
        });

        group.MapDelete("/{waypointId:guid}", async (Guid tourId, Guid waypointId, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new RemoveTourWaypointCommand(tourId, waypointId);
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest();
        });

        group.MapPatch("/{waypointId:guid}/reorder", async (Guid tourId, Guid waypointId, [FromBody] ReorderTourWaypointRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new ReorderTourWaypointCommand(tourId, waypointId, request.NewSortOrder);
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest();
        });
    }
}

public sealed record AddTourWaypointRequest(string Name, string? Description, decimal Latitude, decimal Longitude, int? DurationMinutes, WaypointType WaypointType);
public sealed record ReorderTourWaypointRequest(int NewSortOrder);
