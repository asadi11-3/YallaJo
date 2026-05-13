using ContentTours.Application.Commands.ChildrenInfo.Update;
using ContentTours.Application.Queries.ChildrenInfo.GetTourChildrenInfo;
using ContentTours.Contracts.Authorization;
using ContentTours.Presentation.Endpoints.ChildrenInfo.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.ChildrenInfo;

internal static class ChildrenInfoEndpoints
{
    internal static void MapChildrenInfoEndpoints(RouteGroupBuilder group)
    {
        var info = group.MapGroup("/{id:guid}/children-info")
            .WithTags("ContentTours | ChildrenInfo");

        info.MapGet("/", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTourChildrenInfoQuery(id), ct);
            return result.ToApiResult();
        })
        .AllowAnonymous()
        .WithName("GetTourChildrenInfo")
        .WithSummary("Get children-info block for a tour")
        .Produces<ChildrenInfoDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        info.MapPut("/", async (
            Guid id,
            UpdateChildrenInfoRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateTourChildrenInfoCommand(
                id,
                request.AllowsChildren,
                request.MinChildAge,
                request.MaxChildAge,
                request.ChildFacilities);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateTourChildrenInfo")
        .WithSummary("Update children-related fields on a tour")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourChildrenInfo, AppAction.Update));
    }
}
