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

namespace Security.Presentation;

public static class SecurityEndpoints
{
    private static readonly HashSet<string> _jwtMetaClaims =
        new(StringComparer.Ordinal) { "jti", "iat", "nbf", "exp", "iss", "aud", "sub", "email", "role" };

    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/security")
            .WithTags("Security");

        MapRegisterEndpoint(group);
        MapMeEndpoint(group);
        MapChangePasswordEndpoint(group);
        MapUpdatePhoneEndpoint(group);
        MapCreateRoleEndpoint(group);
        MapListRolesEndpoint(group);
        MapAssignRoleEndpoint(group);
        MapRemoveRoleEndpoint(group);

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
        .AllowAnonymous();
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