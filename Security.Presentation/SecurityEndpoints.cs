using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Application.Commands.CreateUser;
using Security.Application.Commands.VerifyEmail;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Presentation;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/security")
            .WithTags("Security");

        MapUserEndpoints(group);

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

    // ── Result → IResult mapping ──────────────────────────────────────────

    private static IResult ToApiResult<T>(Result<T> result, Func<T, string> locationFactory) =>
        result.IsSuccess
            ? Results.Created(locationFactory(result.Value!), result.Value)
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
