using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Application.Commands.Register;
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

        return endpoints;
    }

    private static void MapRegisterEndpoint(RouteGroupBuilder group)
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

    // ── Result → IResult mapping ──────────────────────────────────────────

    private static IResult ToApiResult<T>(Result<T> result) =>
        result.IsSuccess
            ? result.Outcome == Outcome.Created
                ? Results.Created((string?)null, result.Value)
                : Results.Ok(result.Value)
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

public sealed record RegisterRequest(string Email, string Password);
public sealed record RegisterResponse(Guid UserId, string Message);
