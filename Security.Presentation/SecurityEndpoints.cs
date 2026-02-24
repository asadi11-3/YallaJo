using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Application.Commands.CreateUser;
using Security.Application.Commands.Login;
using Security.Application.Commands.Register;
using Security.Application.Commands.VerifyEmail;
using System.Security.Claims;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Presentation;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/security")
            .WithTags("Security");

        MapUserEndpoints(group);
        MapAuthEndpoints(group);

        return endpoints;
    }

    private static void MapUserEndpoints(RouteGroupBuilder group)
    {
        var users = group.MapGroup("/users");

        users.MapPost("/", async (CreateUserRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateUserCommand(request.Email));
            return ToApiResult(result, id => $"/api/security/users/{id}");
        })
        .WithName("CreateUser")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Register a new security user with an email address");

        users.MapPost("/{userId:guid}/emails/{emailId:guid}/verify",
            async (Guid userId, Guid emailId, ISender sender) =>
        {
            var result = await sender.Send(new VerifyEmailCommand(userId, emailId));
            return ToApiResult(result);
        })
        .WithName("VerifyEmail")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Mark a user's email address as verified");
    }

    private static void MapAuthEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/register", async (RegisterRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RegisterCommand(request.Email, request.Password));
            return ToApiResult(result);
        })
        .WithName("Register")
        .Produces<RegisterResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Register with email and password, returns JWT access token")
        .AllowAnonymous();

        group.MapPost("/login", async (LoginRequest request, ISender sender) =>
        {
            var result = await sender.Send(new LoginCommand(request.Email, request.Password));
            return ToApiResult(result);
        })
        .WithName("Login")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Login with email and password, returns JWT access token")
        .AllowAnonymous();

        group.MapGet("/me", (HttpContext ctx) =>
        {
            var user = ctx.User;
            var userId = user.FindFirstValue("sub");
            var email = user.FindFirstValue("email");
            var roles = user.FindAll("role").Select(c => c.Value).ToList();

            var skipTypes = new HashSet<string>(["jti", "iat", "nbf", "exp", "iss", "aud", "sub", "email", "role"]);
            var extraClaims = user.Claims
                .Where(c => !skipTypes.Contains(c.Type))
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

    // ── Result → IResult mapping ──────────────────────────────────────────

    private static IResult ToApiResult<T>(Result<T> result, Func<T, string> locationFactory) =>
        result.IsSuccess
            ? Results.Created(locationFactory(result.Value!), result.Value)
            : ToProblem(result.Outcome, result.Errors);

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

    private static IResult ToProblem(Outcome outcome, IReadOnlyList<Error> errors) =>
        Results.Problem(
            statusCode: (int)outcome,
            title: errors.FirstOrDefault()?.Code,
            detail: errors.FirstOrDefault()?.Message);
}

// ── Request DTOs (decoupled from Application commands) ────────────────────

public sealed record CreateUserRequest(string Email);
public sealed record RegisterRequest(string Email, string Password);
public sealed record RegisterResponse(Guid UserId, string AccessToken);
public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(Guid UserId, string AccessToken);
