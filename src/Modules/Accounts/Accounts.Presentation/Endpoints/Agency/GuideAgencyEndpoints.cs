using Accounts.Application.Commands.Agency.AcceptInvitation;
using Accounts.Application.Commands.Agency.ApplyToAgency;
using Accounts.Application.Commands.Agency.DeclineInvitation;
using Accounts.Application.Commands.Agency.LeaveAgency;
using Accounts.Application.Queries.Agency.GetAvailableGuides;
using Accounts.Application.Queries.Agency.GetMyInvitations;
using Accounts.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Accounts.Presentation.Endpoints.Agency;

/// <summary>Guide-side endpoints for managing agency relationships.</summary>
public static class GuideAgencyEndpoints
{
    public static void MapGuideAgencyEndpoints(IEndpointRouteBuilder group)
    {
        // GET /api/v1/guides/me/invitations — list pending invitations from agencies
        group.MapGet("/me/invitations", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyInvitationsQuery("received"), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyGuideInvitations")
        .Produces<IReadOnlyList<InvitationDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Get agency invitations received by me as a guide")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.GuideAgency, AppAction.Read))
        .RequireAuthorization();

        // POST /api/v1/guides/agencies/{agencyUserId}/apply — apply to join an agency
        group.MapPost("/agencies/{agencyUserId:guid}/apply", async (Guid agencyUserId, ApplyToAgencyRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ApplyToAgencyCommand(agencyUserId, req.Message), ct);
            return result.ToApiResult();
        })
        .WithName("ApplyToAgency")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary("Apply to join an agency as an independent guide")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.GuideAgency, AppAction.Create))
        .RequireAuthorization();

        // POST /api/v1/guides/invitations/{id}/accept — accept agency invitation
        group.MapPost("/invitations/{id:guid}/accept", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AcceptInvitationCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("AcceptAgencyInvitation")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Accept an agency's invitation")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.GuideAgency, AppAction.Update))
        .RequireAuthorization();

        // POST /api/v1/guides/invitations/{id}/decline — decline agency invitation
        group.MapPost("/invitations/{id:guid}/decline", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeclineInvitationCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeclineAgencyInvitation")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Decline an agency's invitation")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.GuideAgency, AppAction.Update))
        .RequireAuthorization();

        // DELETE /api/v1/guides/me/agency — leave current agency
        group.MapDelete("/me/agency", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LeaveAgencyCommand(), ct);
            return result.ToApiResult();
        })
        .WithName("LeaveAgency")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Leave my current agency")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.GuideAgency, AppAction.Delete))
        .RequireAuthorization();
    }
}

// ── Request Models ────────────────────────────────────────────────────────────

public sealed record ApplyToAgencyRequest(string? Message);
