using ContentTours.Application.Commands.GuideApplication.Apply;
using ContentTours.Application.Commands.GuideApplication.Approve;
using ContentTours.Application.Commands.GuideApplication.Reject;
using ContentTours.Contracts.Authorization;
using ContentTours.Presentation.Endpoints.TourGuide.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.TourGuide;

internal static class GuideApplicationEndpoints
{
    internal static void MapGuideApplicationEndpoints(RouteGroupBuilder group)
    {
        var apps = group.MapGroup("/{tourId:guid}/applications")
            .WithTags("ContentTours | Guide Applications");

        // GET /{tourId}/applications — admin/provider views all applications for their tour
        apps.MapGet("/", async (
            Guid tourId,
            ISender sender,
            CancellationToken ct) =>
        {
            // Simple placeholder — full query handler will be added in Phase 1d query work
            return Results.Ok(Array.Empty<object>());
        })
        .WithName("ListGuideApplications")
        .WithSummary("List all guide applications for a tour")
        .Produces(StatusCodes.Status200OK)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideApplication, AppAction.Read))
        .RequireAuthorization();

        // POST /{tourId}/applications — guide applies to run a tour
        apps.MapPost("/", async (
            Guid tourId,
            ApplyForTourRequest request,
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var cmd = new ApplyForTourCommand(
                tourId,
                request.Message,
                request.RelevantExperience,
                request.ProposedBasePrice,
                request.ProposedScheduleJson);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("ApplyForTour")
        .WithSummary("Apply to run a tour (TourGuide)")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideApplication, AppAction.Create))
        .RequireAuthorization();

        // POST /{tourId}/applications/{applicationId}/approve — admin/provider approves
        apps.MapPost("/{applicationId:guid}/approve", async (
            Guid tourId,
            Guid applicationId,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new ApproveGuideApplicationCommand(applicationId);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("ApproveGuideApplication")
        .WithSummary("Approve a guide application for a tour")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideApplication, AppAction.Approve))
        .RequireAuthorization();

        // POST /{tourId}/applications/{applicationId}/reject — admin/provider rejects
        apps.MapPost("/{applicationId:guid}/reject", async (
            Guid tourId,
            Guid applicationId,
            RejectGuideApplicationRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RejectGuideApplicationCommand(applicationId, request.Reason);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("RejectGuideApplication")
        .WithSummary("Reject a guide application for a tour")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.GuideApplication, AppAction.Reject))
        .RequireAuthorization();

        // POST /{tourId}/open-applications — provider opens tour for guide applications
        group.MapPost("/{tourId:guid}/open-applications", async (
            Guid tourId,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new ContentTours.Application.Commands.Tour.OpenForApplications.OpenTourForApplicationsCommand(tourId);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("OpenTourForApplications")
        .WithSummary("Open a tour for guide applications")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update))
        .RequireAuthorization();

        // POST /{tourId}/close-applications — provider closes tour for guide applications
        group.MapPost("/{tourId:guid}/close-applications", async (
            Guid tourId,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new ContentTours.Application.Commands.Tour.CloseForApplications.CloseTourForApplicationsCommand(tourId);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("CloseTourForApplications")
        .WithSummary("Close a tour for guide applications")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update))
        .RequireAuthorization();
    }
}
