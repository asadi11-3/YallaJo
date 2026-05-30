using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Application.Commands.AddRoleClaim;
using Security.Application.Commands.CreateRole;
using Security.Application.Commands.DeactivateRole;
using Security.Application.Commands.RemoveRoleClaim;
using Security.Application.Commands.UpdateRole;
using Security.Application.Queries.Dtos;
using Security.Application.Queries.GetRole;
using Security.Application.Queries.ListRoles;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using Security.Presentation.Endpoints.Role.Models;
using YallaJo.SharedKernel.Presentation;

namespace Security.Presentation.Endpoints.Role;

internal static class RoleEndpoints
{
    internal static void MapRoleEndpoints(RouteGroupBuilder group)
    {
        var roles = group.MapGroup("/roles");

        roles.MapPost("/", async (CreateRoleRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateRoleCommand(request.Name, request.Description), ct);
            return result.ToApiResult();
        })
        .WithName("CreateRole")
        .Produces<CreateRoleResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a new role")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.Role, AppAction.Create))
        .RequireAuthorization();

        roles.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListRolesQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("ListRoles")
        .Produces<IReadOnlyList<RoleDto>>(StatusCodes.Status200OK)
        .WithSummary("List all active roles")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.Role, AppAction.Read))
        .RequireAuthorization();

        roles.MapGet("/{roleId:guid}", async (Guid roleId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRoleQuery(roleId), ct);
            return result.ToApiResult();
        })
        .WithName("GetRole")
        .Produces<RoleDetailsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a role with its claims")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.Role, AppAction.Read))
        .RequireAuthorization();

        roles.MapPatch("/{roleId:guid}", async (Guid roleId, UpdateRoleRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateRoleCommand(roleId, request.Description), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateRole")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithSummary("Update a role's description")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.Role, AppAction.Update))
        .RequireAuthorization();

        roles.MapPatch("/{roleId:guid}/deactivate", async (Guid roleId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeactivateRoleCommand(roleId), ct);
            return result.ToApiResult();
        })
        .WithName("DeactivateRole")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithSummary("Deactivate a role")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.Role, AppAction.Update))
        .RequireAuthorization();

        roles.MapPost("/{roleId:guid}/claims", async (Guid roleId, AddRoleClaimRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddRoleClaimCommand(roleId, request.ClaimType, request.ClaimValue), ct);
            return result.ToApiResult();
        })
        .WithName("AddRoleClaim")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Add a claim to a role")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.RoleClaim, AppAction.Create))
        .RequireAuthorization();

        roles.MapDelete("/{roleId:guid}/claims/{claimId:guid}", async (Guid roleId, Guid claimId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveRoleClaimCommand(roleId, claimId), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveRoleClaim")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove a claim from a role")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.RoleClaim, AppAction.Delete))
        .RequireAuthorization();
    }
}
