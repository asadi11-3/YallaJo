using Accounts.Application.Commands.Agency.ApproveGuideApplication;
using Accounts.Application.Commands.Agency.InviteGuide;
using Accounts.Application.Commands.Agency.RejectGuideApplication;
using Accounts.Application.Commands.Agency.RemoveGuide;
using Accounts.Application.Queries.Agency.GetAgencyApplications;
using Accounts.Application.Queries.Agency.GetMyAgencyGuides;
using Accounts.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Accounts.Presentation.Endpoints.Agency;

/// <summary>Agency-side endpoints for managing the guide roster.</summary>
public static class AgencyEndpoints
{
    public static void MapAgencyEndpoints(IEndpointRouteBuilder group)
    {
        // GET /api/v1/agency/guides — list my guides
        group.MapGet("/guides", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyAgencyGuidesQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyAgencyGuides")
        .Produces<IReadOnlyList<AgencyGuideDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Get all guides affiliated with my agency")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AgencyRoster, AppAction.Read))
        .RequireAuthorization();

        // GET /api/v1/agency/applications — list guide applications to my agency
        group.MapGet("/applications", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAgencyApplicationsQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetAgencyApplications")
        .Produces<IReadOnlyList<AgencyApplicationDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Get guide applications to my agency")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AgencyRoster, AppAction.Read))
        .RequireAuthorization();

        // POST /api/v1/agency/guides/invite — invite a guide
        group.MapPost("/guides/invite", async (InviteGuideRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new InviteGuideCommand(
                req.GuideUserId,
                req.Message,
                req.ProposedCommissionPercentage), ct);
            return result.ToApiResult();
        })
        .WithName("InviteGuide")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Invite an independent guide to join the agency")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AgencyRoster, AppAction.Create))
        .RequireAuthorization();

        // POST /api/v1/agency/applications/{id}/approve — approve a guide application
        group.MapPost("/applications/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveGuideApplicationCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("ApproveGuideApplication")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Approve a guide's application to join the agency")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AgencyRoster, AppAction.Approve))
        .RequireAuthorization();

        // POST /api/v1/agency/applications/{id}/reject — reject a guide application
        group.MapPost("/applications/{id:guid}/reject", async (Guid id, RejectGuideApplicationRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RejectGuideApplicationCommand(id, req.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("RejectGuideApplication")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Reject a guide's application to join the agency")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AgencyRoster, AppAction.Reject))
        .RequireAuthorization();

        // DELETE /api/v1/agency/guides/{guideUserId} — remove guide from agency
        group.MapDelete("/guides/{guideUserId:guid}", async (Guid guideUserId, RemoveGuideRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveGuideCommand(guideUserId, req.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveGuideFromAgency")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove a guide from the agency roster")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AgencyRoster, AppAction.Delete))
        .RequireAuthorization();
    }
}

// ── Request Models ────────────────────────────────────────────────────────────

public sealed record InviteGuideRequest(
    Guid GuideUserId,
    string? Message,
    decimal ProposedCommissionPercentage);

public sealed record RejectGuideApplicationRequest(string Reason);

public sealed record RemoveGuideRequest(string Reason);
