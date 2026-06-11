using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Application.Commands.ActivateUser;
using Security.Application.Commands.AddUserClaim;
using Security.Application.Commands.AssignRole;
using Security.Application.Commands.DeactivateUser;
using Security.Application.Commands.RemoveRole;
using Security.Application.Commands.RemoveUserClaim;
using Security.Application.Queries.Dtos;
using Security.Application.Queries.GetSecurityMe;
using Security.Application.Queries.GetUser;
using Security.Application.Queries.ListUsers;
using Security.Application.Queries.LookupUsers;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using Security.Presentation.Endpoints.User.Models;
using System.Security.Claims;
using YallaJo.SharedKernel.Presentation;

namespace Security.Presentation.Endpoints.User;

internal static class UserEndpoints
{
    internal static void MapUserEndpoints(RouteGroupBuilder group)
    {
        MapMeEndpoint(group);
        MapListUsersEndpoint(group);
        MapLookupUsersEndpoint(group);
        MapGetUserEndpoint(group);
        MapActivateUserEndpoint(group);
        MapDeactivateUserEndpoint(group);
        MapAssignRoleEndpoint(group);
        MapRemoveRoleEndpoint(group);
        MapAddUserClaimEndpoint(group);
        MapRemoveUserClaimEndpoint(group);
    }

    private static void MapMeEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/me", async (HttpContext ctx, ISender sender, CancellationToken ct) =>
        {
            var sub = ctx.User.FindFirstValue("sub");
            if (!Guid.TryParse(sub, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await sender.Send(new GetSecurityMeQuery(userId), ct);
            return result.ToApiResult();
        })
        .WithName("Me")
        .Produces<SecurityMeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Returns the current user's DB-backed roles & permissions snapshot")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.Read))
        .RequireAuthorization();
    }

    private static void MapListUsersEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/users", async (ISender sender, CancellationToken ct, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new ListUsersQuery(page, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("ListUsers")
        .Produces<PagedUsersResponse>(StatusCodes.Status200OK)
        .WithSummary("List users with pagination")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.Read))
        .RequireAuthorization();
    }

    /// <summary>
    /// [Backend] B1: lightweight user lookup for typeahead pickers and batch identity
    /// enrichment. Auth-only (no admin permission); anti-enumeration mitigations:
    /// min query length 2, result cap 20, ids cap 50, bare requests rejected with 400,
    /// and the DTO carries no roles/claims/state.
    /// </summary>
    private static void MapLookupUsersEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/users/lookup", async (
            ISender sender,
            CancellationToken ct,
            string? q = null,
            string? ids = null,
            int limit = 10) =>
        {
            var query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
            if (query is { Length: < 2 })
            {
                return Results.BadRequest(new { error = "Query must be at least 2 characters." });
            }

            List<Guid>? idList = null;
            if (!string.IsNullOrWhiteSpace(ids))
            {
                idList = [];
                foreach (var token in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!Guid.TryParse(token, out var parsed))
                    {
                        return Results.BadRequest(new { error = "ids must be a comma-separated list of GUIDs." });
                    }

                    idList.Add(parsed);
                }

                if (idList.Count > 50)
                {
                    return Results.BadRequest(new { error = "At most 50 ids are allowed per request." });
                }
            }

            if (query is null && idList is not { Count: > 0 })
            {
                return Results.BadRequest(new { error = "Provide a search query (q) or an id list (ids)." });
            }

            var clampedLimit = Math.Clamp(limit, 1, 20);
            var effectiveLimit = idList is { Count: > 0 }
                ? Math.Max(clampedLimit, idList.Count) // batch enrichment must return every requested id
                : clampedLimit;

            var result = await sender.Send(new LookupUsersQuery(query, idList, effectiveLimit), ct);
            return result.ToApiResult();
        })
        .WithName("LookupUsers")
        .Produces<IReadOnlyList<UserLookupDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Typeahead/batch user lookup by email or ids (max 20 rows; auth required)")
        .RequireAuthorization();
    }

    private static void MapGetUserEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/users/{userId:guid}", async (Guid userId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserQuery(userId), ct);
            return result.ToApiResult();
        })
        .WithName("GetUser")
        .Produces<UserDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a user by ID")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.Read))
        .RequireAuthorization();
    }

    private static void MapActivateUserEndpoint(RouteGroupBuilder group)
    {
        group.MapPatch("/users/{userId:guid}/activate", async (Guid userId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ActivateUserCommand(userId), ct);
            return result.ToApiResult();
        })
        .WithName("ActivateUser")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Activate a user account")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();
    }

    private static void MapDeactivateUserEndpoint(RouteGroupBuilder group)
    {
        group.MapPatch("/users/{userId:guid}/deactivate", async (Guid userId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeactivateUserCommand(userId), ct);
            return result.ToApiResult();
        })
        .WithName("DeactivateUser")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Deactivate a user account")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();
    }

    private static void MapAssignRoleEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/users/{userId:guid}/roles", async (Guid userId, AssignRoleRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AssignRoleCommand(userId, request.RoleId), ct);
            return result.ToApiResult();
        })
        .WithName("AssignRole")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithSummary("Assign a role to a user")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.UserRole, AppAction.Create))
        .RequireAuthorization();
    }

    private static void MapRemoveRoleEndpoint(RouteGroupBuilder group)
    {
        group.MapDelete("/users/{userId:guid}/roles/{roleId:guid}", async (Guid userId, Guid roleId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveRoleCommand(userId, roleId), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveRole")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithSummary("Remove a role from a user")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.UserRole, AppAction.Delete))
        .RequireAuthorization();
    }

    private static void MapAddUserClaimEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/users/{userId:guid}/claims", async (Guid userId, AddUserClaimRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddUserClaimCommand(userId, request.ClaimType, request.ClaimValue), ct);
            return result.ToApiResult();
        })
        .WithName("AddUserClaim")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Add a claim to a user")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();
    }

    private static void MapRemoveUserClaimEndpoint(RouteGroupBuilder group)
    {
        group.MapDelete("/users/{userId:guid}/claims/{claimId:guid}", async (Guid userId, Guid claimId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveUserClaimCommand(userId, claimId), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveUserClaim")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove a claim from a user")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();
    }
}
