using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Application.Commands.ActivateUser;
using Security.Application.Commands.AddUserClaim;
using Security.Application.Commands.AssignRole;
using Security.Application.Commands.DeactivateUser;
using Security.Application.Commands.Register;
using Security.Application.Commands.RemoveRole;
using Security.Application.Commands.RemoveUserClaim;
using Security.Application.Queries.Dtos;
using Security.Application.Queries.GetUser;
using Security.Application.Queries.ListUsers;
using Security.Contracts.Authorization;
using Security.Presentation.Endpoints.User.Models;
using System.Security.Claims;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation;

namespace Security.Presentation.Endpoints.User;

internal static class UserEndpoints
{
    // Matches Auth.Presentation.RateLimitPolicies.RegisterPolicy.
    // Defined locally to avoid a cross-module project reference.
    private const string RegisterRateLimitPolicy = "register-rate-limit";

    private static readonly HashSet<string> _jwtMetaClaims =
        new(StringComparer.Ordinal) { "jti", "iat", "nbf", "exp", "iss", "aud", "sub", "email", "role" };

    internal static void MapUserEndpoints(RouteGroupBuilder group)
    {
        MapRegisterEndpoint(group);
        MapMeEndpoint(group);
        MapListUsersEndpoint(group);
        MapGetUserEndpoint(group);
        MapActivateUserEndpoint(group);
        MapDeactivateUserEndpoint(group);
        MapAssignRoleEndpoint(group);
        MapRemoveRoleEndpoint(group);
        MapAddUserClaimEndpoint(group);
        MapRemoveUserClaimEndpoint(group);
    }

    // ── Anonymous ─────────────────────────────────────────────────────────────

    private static void MapRegisterEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/register", async (RegisterRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new RegisterCommand(request.FirstName, request.LastName, request.Email, request.Password), ct);
            return result.ToApiResult();
        })
        .WithName("Register")
        .Produces<RegisterResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Register a new account — verification email will be sent")
        .AllowAnonymous()
        .RequireRateLimiting(RegisterRateLimitPolicy);
    }

    private static void MapMeEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/me", (HttpContext ctx) =>
        {
            var user        = ctx.User;
            var userId      = user.FindFirstValue("sub");
            var email       = user.FindFirstValue("email");
            var roles       = user.FindAll("role").Select(c => c.Value).ToList();
            var extraClaims = user.Claims
                .Where(c => !_jwtMetaClaims.Contains(c.Type))
                .Select(c => new { c.Type, c.Value })
                .ToList();

            return Results.Ok(new { userId, email, roles, claims = extraClaims });
        })
        .WithName("Me")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Returns claims from the current user's JWT")
        .RequireAuthorization();
    }

    // ── Admin: user management (/users) ───────────────────────────────────────

    private static void MapListUsersEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/users", async (ISender sender, CancellationToken ct, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new ListUsersQuery(page, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("ListUsers")
        .Produces<PaginatedResult<UserDto>>(StatusCodes.Status200OK)
        .WithSummary("List users with pagination")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Read))
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
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Read))
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
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.UpdateAny))
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
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.UpdateAny))
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
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.UserRole, AppAction.Create))
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
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.UserRole, AppAction.Delete))
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
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.UpdateAny))
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
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();
    }
}
