using System;
using System.Threading;
using ContentTours.Application.Commands.TourGuides.Assign;
using ContentTours.Application.Commands.TourGuides.Unassign;
using ContentTours.Application.Features.TourGuides.Queries.GetByTourId;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ContentTours.Presentation.Endpoints;

public static class TourGuidesEndpoints
{
    public static void MapTourGuidesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tours/{tourId:guid}/guides");

        group.MapGet("/", async (Guid tourId, ISender sender, CancellationToken cancellationToken) =>
        {
            var query = new GetTourGuidesQuery(tourId);
            var result = await sender.Send(query, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/", async (Guid tourId, [FromBody] AssignTourGuideRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new AssignTourGuideCommand(tourId, request.TourGuideUserId, request.IsPrimary);
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest();
        });

        group.MapDelete("/{guideUserId:guid}", async (Guid tourId, Guid guideUserId, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new UnassignTourGuideCommand(tourId, guideUserId);
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest();
        });
    }
}

public sealed record AssignTourGuideRequest(Guid TourGuideUserId, bool IsPrimary);
