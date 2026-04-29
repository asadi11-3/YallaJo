using ContentTours.Application.Commands.ChildrenInfo.Update;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ContentTours.Presentation.Endpoints;

public static class ChildrenInfoEndpoints
{
    public static void MapChildrenInfoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tours/{tourId:guid}/children-info");

        group.MapPut("/", async (Guid tourId, [FromBody] UpdateChildrenInfoRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new UpdateTourChildrenInfoCommand(tourId, request.IsChildFriendly, request.AgeRestriction, request.MinChildAge, request.MaxChildAge, request.ChildFacilities);
            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });
    }
}

public sealed record UpdateChildrenInfoRequest(bool IsChildFriendly, int? AgeRestriction, int? MinChildAge, int? MaxChildAge, string? ChildFacilities);
