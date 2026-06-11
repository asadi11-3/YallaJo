using ContentTours.Application.Commands.TourGuides.Assign;
using ContentTours.Application.Commands.TourGuides.Unassign;
using ContentTours.Application.Queries.TourGuides;
using ContentTours.Application.Queries.TourGuides.LookupAssignableGuides;
using ContentTours.Contracts.Authorization;
using ContentTours.Presentation.Endpoints.TourGuide.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.TourGuide;

internal static class TourGuideEndpoints
{
    internal static void MapTourGuideEndpoints(RouteGroupBuilder group)
    {
        var guides = group.MapGroup("/{id:guid}/guides")
            .WithTags("ContentTours | Guides");

        // GET /{id:guid}/guides — public read (AllowAnonymous per PDF B5)
        guides.MapGet("/", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTourGuidesQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("ListGuidesAssignedToTour")
        .WithSummary("List tour guides assigned to a tour (primary first, then by Id)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // GET /{id:guid}/guides/lookup — [Backend] B7 typeahead for the assign-guide picker (F10)
        guides.MapGet("/lookup", async (
            Guid id,
            ISender sender,
            CancellationToken ct,
            string? term = null,
            int pageSize = 10) =>
        {
            var result = await sender.Send(new LookupAssignableGuidesQuery(id, term, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("LookupAssignableGuides")
        .WithSummary("Typeahead lookup of active guides eligible for assignment to this tour (max 10 rows)")
        .Produces<IReadOnlyList<GuideLookupDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update))
        .RequireAuthorization();

        // POST /{id:guid}/guides — assign a guide (owner or admin)
        guides.MapPost("/", async (
            Guid id,
            AssignTourGuideRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new AssignTourGuideCommand(id, request.TourGuideUserId, request.IsPrimary);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AssignTourGuide")
        .WithSummary("Assign a tour guide to a tour (auto-promotes to primary on first assignment)")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update));

        // DELETE /{id:guid}/guides/{guideUserId:guid} — unassign (owner or admin)
        guides.MapDelete("/{guideUserId:guid}", async (
            Guid id,
            Guid guideUserId,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UnassignTourGuideCommand(id, guideUserId);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UnassignTourGuide")
        .WithSummary("Remove a tour guide assignment (promotes next guide to primary if needed)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update));
    }
}
