using ContentTours.Application.Commands.TourProposal.Approve;
using ContentTours.Application.Commands.TourProposal.Create;
using ContentTours.Application.Commands.TourProposal.Reject;
using ContentTours.Application.Commands.TourProposal.Submit;
using ContentTours.Application.Queries.TourProposal.Common;
using ContentTours.Application.Queries.TourProposal.GetTourProposalById;
using ContentTours.Application.Queries.TourProposal.ListTourProposals;
using ContentTours.Contracts.Authorization;
using ContentTours.Domain.Enums;
using ContentTours.Presentation.Endpoints.TourGuide.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.TourGuide;

internal static class TourProposalEndpoints
{
    internal static void MapTourProposalEndpoints(IEndpointRouteBuilder endpoints)
    {
        var proposals = endpoints.MapGroup("/api/v1/tours/proposals")
            .WithTags("ContentTours | Tour Proposals");

        // WS-5b (Phase 3 G3b): real list handler — supports guide-scoped (?guideId=&status=) + admin pending queue.
        proposals.MapGet("/", async (
            Guid? guideId,
            TourProposalStatus? status,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListTourProposalsQuery(guideId, status), ct);
            return result.ToApiResult();
        })
        .WithName("ListTourProposals")
        .WithSummary("List tour proposals (guideId omitted = admin pending queue; with guideId = that guide's own, optional status filter)")
        .Produces<IReadOnlyList<TourProposalDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourProposal, AppAction.Read))
        .RequireAuthorization();

        // WS-5b (Phase 3 G3b): get a single proposal by id.
        proposals.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTourProposalByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetTourProposalById")
        .WithSummary("Get a single tour proposal by id")
        .Produces<TourProposalDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourProposal, AppAction.Read))
        .RequireAuthorization();

        // POST /tours/proposals — guide creates a proposal draft
        proposals.MapPost("/", async (
            CreateTourProposalRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateTourProposalCommand(
                request.Title,
                request.Description,
                request.ShortDescription,
                request.PlaceId,
                request.DurationMinutes,
                request.MaxGroupSize,
                request.BasePrice,
                request.Currency,
                request.RequestExclusive);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("CreateTourProposal")
        .WithSummary("Create a tour proposal draft (TourGuide)")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourProposal, AppAction.Create))
        .RequireAuthorization();

        // POST /tours/proposals/{id}/submit — guide submits for admin review
        proposals.MapPost("/{id:guid}/submit", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new SubmitTourProposalCommand(id);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("SubmitTourProposal")
        .WithSummary("Submit a tour proposal for admin review")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourProposal, AppAction.Submit))
        .RequireAuthorization();

        // POST /tours/proposals/{id}/approve — admin approves and creates the tour
        proposals.MapPost("/{id:guid}/approve", async (
            Guid id,
            ApproveTourProposalRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new ApproveTourProposalCommand(id, request.IsExclusive);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("ApproveTourProposal")
        .WithSummary("Approve a tour proposal and create the tour (Admin)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourProposal, AppAction.Approve))
        .RequireAuthorization();

        // POST /tours/proposals/{id}/reject — admin rejects
        proposals.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectTourProposalRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RejectTourProposalCommand(id, request.Reason);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("RejectTourProposal")
        .WithSummary("Reject a tour proposal (Admin)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourProposal, AppAction.Reject))
        .RequireAuthorization();
    }
}
