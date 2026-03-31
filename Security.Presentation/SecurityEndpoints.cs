using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Application.Commands.AssignRole;
using Security.Application.Commands.ChangePassword;
using Security.Application.Commands.CreateRole;
using Security.Application.Commands.Register;
using Security.Application.Commands.RemoveRole;
using Security.Application.Commands.UpdatePhone;
using Security.Application.Queries.ListRoles;
using Security.Contracts.Authorization;
using System.Security.Claims;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Security.Application.Commands.ActivateUser;
using Security.Application.Commands.DeactivateRole;
using Security.Application.Commands.DeactivateUser;
using Security.Application.Commands.UpdateRole;
using Security.Application.Queries.GetAuditLogs;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using Security.Application.Commands.AddRoleClaim;
using Security.Application.Commands.RemoveRoleClaim;
using Security.Application.Commands.AddUserClaim;
using Security.Application.Commands.RemoveUserClaim;
using Security.Application.Queries.GetUser;
using Security.Application.Queries.ListUsers;
using Security.Application.Queries.Dtos;

namespace Security.Presentation;

public static class SecurityEndpoints
{
    private static readonly HashSet<string> _jwtMetaClaims =
        new(StringComparer.Ordinal) { "jti", "iat", "nbf", "exp", "iss", "aud", "sub", "email", "role" };

    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/security")
            .WithTags("Security");

        MapRegisterEndpoint(group);
        MapMeEndpoint(group);
        MapChangePasswordEndpoint(group);
        MapUpdatePhoneEndpoint(group);
        MapCreateRoleEndpoint(group);
        MapListRolesEndpoint(group);
        MapAssignRoleEndpoint(group);
        MapRemoveRoleEndpoint(group);
        MapGetAuditLogsEndpoint(group);
        MapDeactivateUserEndpoint(group);
        MapActivateUserEndpoint(group);
        MapDeactivateRoleEndpoint(group);
        MapUpdateRoleEndpoint(group);
        MapGetUserEndpoint(group);
        MapListUsersEndpoint(group);
        MapAddRoleClaimEndpoint(group);
        MapRemoveRoleClaimEndpoint(group);
        MapAddUserClaimEndpoint(group);
        MapRemoveUserClaimEndpoint(group);

        return endpoints;
    }

    private static void MapRegisterEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/register", async (RegisterRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RegisterCommand(request.FirstName, request.LastName, request.Email, request.Password));
            return ToApiResult(result);
        })
        .WithName("Register")
        .Produces<RegisterResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Register a new account — verification email will be sent")
        .AllowAnonymous()
        .RequireRateLimiting("register-rate-limit");
    }

    private static void MapMeEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/me", (HttpContext ctx) =>
        {
            var user = ctx.User;
            var userId = user.FindFirstValue("sub");
            var email = user.FindFirstValue("email");
            var roles = user.FindAll("role").Select(c => c.Value).ToList();

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

    private static void MapChangePasswordEndpoint(RouteGroupBuilder group)
    {
        var account = group.MapGroup("/account");

        account.MapPut("/password", async (ChangePasswordRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ChangePasswordCommand(
                request.CurrentPassword,
                request.NewPassword,
                request.ConfirmNewPassword));
            return ToApiResult(result);
        })
        .WithName("ChangePassword")
        .Produces<ChangePasswordResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Change the current user's password")
        .RequireAuthorization();
    }

    private static void MapUpdatePhoneEndpoint(RouteGroupBuilder group)
    {
        var account = group.MapGroup("/account");

        account.MapPut("/phone", async (UpdatePrimaryPhoneRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdatePrimaryPhoneCommand(request.PhoneNumber));
            return ToApiResult(result);
        })
        .WithName("UpdatePrimaryPhone")
        .Produces<UpdatePrimaryPhoneResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update the current user's primary phone number")
        .RequireAuthorization();
    }
    private static void MapCreateRoleEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/roles", async (CreateRoleRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateRoleCommand(request.Name, request.Description));
            return ToApiResult(result);
        })
        .WithName("CreateRole")
        .Produces<CreateRoleResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a new role")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Role, AppAction.Create))
        .RequireAuthorization();
    }

    private static void MapListRolesEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/roles", async (ISender sender) =>
        {
            var result = await sender.Send(new ListRolesQuery());
            return ToApiResult(result);
        })
        .WithName("ListRoles")
        .Produces<IReadOnlyList<RoleDto>>(StatusCodes.Status200OK)
        .WithSummary("List all active roles")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Role, AppAction.Read))
        .RequireAuthorization();
    }

    private static void MapAssignRoleEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/users/{userId:guid}/roles", async (Guid userId, AssignRoleRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AssignRoleCommand(userId, request.RoleId));
            return ToApiResult(result);
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
        group.MapDelete("/users/{userId:guid}/roles/{roleId:guid}", async (Guid userId, Guid roleId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveRoleCommand(userId, roleId));
            return ToApiResult(result);
        })
        .WithName("RemoveRole")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithSummary("Remove a role from a user")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.UserRole, AppAction.Delete))
        .RequireAuthorization();
    }

    private static void MapGetAuditLogsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/audit-logs", async (ISender sender, int page = 1, int pageSize = 20, Guid? userId = null) =>
        {
            var result = await sender.Send(new GetAuditLogsQuery(page, pageSize, userId));
            return ToApiResult(result);
        })
        .WithName("GetAuditLogs")
        .Produces<PaginatedResult<AuditLogDto>>(StatusCodes.Status200OK)
        .WithSummary("Get paginated audit logs, optionally filtered by user")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.System, AppAction.Read))
        .RequireAuthorization();
    }

    private static void MapDeactivateUserEndpoint(RouteGroupBuilder group)
    {
        group.MapPatch("/users/{userId:guid}/deactivate", async (Guid userId, ISender sender) =>
        {
            var result = await sender.Send(new DeactivateUserCommand(userId));
            return ToApiResult(result);
        })
        .WithName("DeactivateUser")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Deactivate a user account")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Update))
        .RequireAuthorization();
    }

    private static void MapActivateUserEndpoint(RouteGroupBuilder group)
    {
        group.MapPatch("/users/{userId:guid}/activate", async (Guid userId, ISender sender) =>
        {
            var result = await sender.Send(new ActivateUserCommand(userId));
            return ToApiResult(result);
        })
        .WithName("ActivateUser")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Activate a user account")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Update))
        .RequireAuthorization();
    }

    private static void MapDeactivateRoleEndpoint(RouteGroupBuilder group)
    {
        group.MapPatch("/roles/{roleId:guid}/deactivate", async (Guid roleId, ISender sender) =>
        {
            var result = await sender.Send(new DeactivateRoleCommand(roleId));
            return ToApiResult(result);
        })
        .WithName("DeactivateRole")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithSummary("Deactivate a role")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Role, AppAction.Update))
        .RequireAuthorization();
    }

    private static void MapUpdateRoleEndpoint(RouteGroupBuilder group)
    {
        group.MapPatch("/roles/{roleId:guid}", async (Guid roleId, UpdateRoleRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateRoleCommand(roleId, request.Description));
            return ToApiResult(result);
        })
        .WithName("UpdateRole")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithSummary("Update a role's description")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.Role, AppAction.Update))
        .RequireAuthorization();
    }

    private static void MapGetUserEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/users/{userId:guid}", async (Guid userId, ISender sender) =>
        {
            var result = await sender.Send(new GetUserQuery(userId));
            return ToApiResult(result);
        })
        .WithName("GetUser")
        .Produces<UserDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a user by ID")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Read))
        .RequireAuthorization();
    }

    private static void MapListUsersEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/users", async (ISender sender, int page = 1, int pageSize = 20) =>
        {
            var result = await sender.Send(new ListUsersQuery(page, pageSize));
            return ToApiResult(result);
        })
        .WithName("ListUsers")
        .Produces<PaginatedResult<UserDto>>(StatusCodes.Status200OK)
        .WithSummary("List users with pagination")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Read))
        .RequireAuthorization();
    }

    private static void MapAddRoleClaimEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/roles/{roleId:guid}/claims", async (Guid roleId, AddClaimRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AddRoleClaimCommand(roleId, request.ClaimType, request.ClaimValue));
            return ToApiResult(result);
        })
        .WithName("AddRoleClaim")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Add a claim to a role")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.RoleClaim, AppAction.Create))
        .RequireAuthorization();
    }

    private static void MapRemoveRoleClaimEndpoint(RouteGroupBuilder group)
    {
        group.MapDelete("/roles/{roleId:guid}/claims/{claimId:guid}", async (Guid roleId, Guid claimId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveRoleClaimCommand(roleId, claimId));
            return ToApiResult(result);
        })
        .WithName("RemoveRoleClaim")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove a claim from a role")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.RoleClaim, AppAction.Delete))
        .RequireAuthorization();
    }

    private static void MapAddUserClaimEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/users/{userId:guid}/claims", async (Guid userId, AddClaimRequest request, ISender sender) =>
        {
            var result = await sender.Send(new AddUserClaimCommand(userId, request.ClaimType, request.ClaimValue));
            return ToApiResult(result);
        })
        .WithName("AddUserClaim")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Add a claim to a user")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Update))
        .RequireAuthorization();
    }

    private static void MapRemoveUserClaimEndpoint(RouteGroupBuilder group)
    {
        group.MapDelete("/users/{userId:guid}/claims/{claimId:guid}", async (Guid userId, Guid claimId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveUserClaimCommand(userId, claimId));
            return ToApiResult(result);
        })
        .WithName("RemoveUserClaim")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove a claim from a user")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Update))
        .RequireAuthorization();
    }

    // ── Result → IResult mapping ──────────────────────────────────────────

    private static IResult ToApiResult<T>(Result<T> result) =>
        result.IsSuccess
            ? result.Outcome == Outcome.Created
                ? Results.Created((string?)null, result.Value)
                : Results.Ok(result.Value)
            : ToProblem(result.Outcome, result.Errors);

    private static IResult ToApiResult(Result result) =>
       result.IsSuccess
           ? Results.Ok()
           : ToProblem(result.Outcome, result.Errors);
    private static IResult ToProblem(Outcome outcome, IReadOnlyList<Error> errors)
    {
        var first = errors.Count > 0 ? errors[0] : null;
        return Results.Problem(
            statusCode: (int)outcome,
            title: first?.Code,
            detail: first?.Message);
    }
}

// ── Request/Response DTOs ────────────────────────────────────────────────

public sealed record RegisterRequest(string FirstName, string LastName, string Email, string Password);
public sealed record RegisterResponse(Guid UserId, string Message);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
public sealed record UpdatePrimaryPhoneRequest(string PhoneNumber);
public sealed record CreateRoleRequest(string Name, string? Description);
public sealed record AssignRoleRequest(Guid RoleId);
public sealed record UpdateRoleRequest(string? Description);
public sealed record AddClaimRequest(string ClaimType, string ClaimValue);
