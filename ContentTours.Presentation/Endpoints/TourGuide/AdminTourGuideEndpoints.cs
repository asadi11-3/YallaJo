using ContentTours.Application.Commands.TourGuides.AdminDeactivateGuide;
using ContentTours.Application.Commands.TourGuides.AdminUpdateGuide;
using ContentTours.Application.Commands.TourGuides.ReinstateGuide;
using ContentTours.Application.Commands.TourGuides.SuspendGuide;
using ContentTours.Application.Queries.TourGuides.GetById;
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

internal static class AdminTourGuideEndpoints
{
    internal static void MapAdminTourGuideEndpoints(RouteGroupBuilder group)
    {
        var admin = group.MapGroup("/admin/{guideId:guid}")
            .WithTags("ContentTours | Admin Guide Management");

        // GET /guides/admin/{guideId} — admin views full guide profile
        admin.MapGet("/", async (
            Guid guideId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTourGuideByIdQuery(guideId), ct);
            return result.ToApiResult();
        })
        .WithName("AdminGetTourGuide")
        .WithSummary("Admin: Get full tour guide profile")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
        .RequireAuthorization();

        // POST /guides/admin/{guideId}/suspend — admin suspends a guide
        admin.MapPost("/suspend", async (
            Guid guideId,
            SuspendTourGuideRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new SuspendTourGuideCommand(guideId, request.Reason);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("SuspendTourGuide")
        .WithSummary("Admin: Suspend a tour guide")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Suspend))
        .RequireAuthorization();

        // POST /guides/admin/{guideId}/reinstate — admin reinstates a suspended guide
        admin.MapPost("/reinstate", async (
            Guid guideId,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new ReinstateTourGuideCommand(guideId);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("ReinstateTourGuide")
        .WithSummary("Admin: Reinstate a suspended tour guide")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Reinstate))
        .RequireAuthorization();

        // PUT /guides/admin/{guideId} — admin updates a guide profile
        admin.MapPut("/", async (
            Guid guideId,
            AdminUpdateTourGuideRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new AdminUpdateTourGuideCommand(
                guideId,
                request.Bio,
                request.YearsOfExperience,
                request.HasFirstAid,
                request.MoTALicenseNumber);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminUpdateTourGuide")
        .WithSummary("Admin: Update a tour guide profile")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Update))
        .RequireAuthorization();

        // DELETE /guides/admin/{guideId} — admin deactivates a guide
        admin.MapDelete("/", async (
            Guid guideId,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new AdminDeactivateTourGuideCommand(guideId);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminDeactivateTourGuide")
        .WithSummary("Admin: Deactivate a tour guide")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Delete))
        .RequireAuthorization();
    }
}
