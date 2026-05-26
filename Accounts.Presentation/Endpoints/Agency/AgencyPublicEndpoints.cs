using Accounts.Application.Queries.Agency.GetAgencies;
using Accounts.Application.Queries.Agency.GetAgencyDetail;
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

/// <summary>Public and agency-facing endpoints for discovery.</summary>
public static class AgencyPublicEndpoints
{
    public static void MapAgencyPublicEndpoints(IEndpointRouteBuilder group)
    {
        // GET /api/v1/agency — list approved agencies accepting guide applications
        group.MapGet("/", async (int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAgenciesQuery(page, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetAgencies")
        .Produces<GetAgenciesResult>(StatusCodes.Status200OK)
        .WithSummary("List approved agencies accepting guide applications")
        .AllowAnonymous();

        // GET /api/v1/agency/{agencyUserId} — public agency detail
        group.MapGet("/{agencyUserId:guid}", async (Guid agencyUserId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAgencyDetailQuery(agencyUserId), ct);
            return result.ToApiResult();
        })
        .WithName("GetAgencyDetail")
        .Produces<AgencyDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get public detail of a specific agency")
        .AllowAnonymous();

        // GET /api/v1/agency/guides/available — list available independent guides not affiliated with any agency
        group.MapGet("/guides/available", async (int page, int pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAvailableGuidesQuery(page, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetAvailableGuides")
        .Produces<IReadOnlyList<AvailableGuideDto>>(StatusCodes.Status200OK)
        .WithSummary("Get independent guides available for agency affiliation (not yet affiliated)")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AgencyRoster, AppAction.Read))
        .RequireAuthorization();

        // GET /api/v1/agency/invitations/sent — list invitations sent by my agency
        group.MapGet("/invitations/sent", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyInvitationsQuery("sent"), ct);
            return result.ToApiResult();
        })
        .WithName("GetSentAgencyInvitations")
        .Produces<IReadOnlyList<InvitationDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Get invitations sent by my agency to guides")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.AgencyRoster, AppAction.Read))
        .RequireAuthorization();
    }
}
