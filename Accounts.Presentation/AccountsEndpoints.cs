using Accounts.Application.Commands.CreateProfile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Presentation;

public static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/accounts")
            .WithTags("Accounts");

        MapProfileEndpoints(group);

        return endpoints;
    }

    private static void MapProfileEndpoints(RouteGroupBuilder group)
    {
        var profiles = group.MapGroup("/profiles");

        profiles.MapPost("/", async (CreateProfileRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateProfileCommand(
                request.UserId,
                request.FirstName,
                request.LastName,
                request.DisplayName,
                request.AvatarUrl));

            return ToApiResult(result, id => $"/api/accounts/profiles/{id}");
        })
        .WithName("CreateProfile")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a user profile linked to an existing security user");
    }

    // ── Result → IResult mapping ──────────────────────────────────────────

    private static IResult ToApiResult<T>(Result<T> result, Func<T, string> locationFactory) =>
        result.IsSuccess
            ? Results.Created(locationFactory(result.Value!), result.Value)
            : ToProblem(result.Outcome, result.Errors);

    private static IResult ToProblem(Outcome outcome, IReadOnlyList<Error> errors) =>
        Results.Problem(
            statusCode: (int)outcome,
            title: errors.FirstOrDefault()?.Code,
            detail: errors.FirstOrDefault()?.Message);
}

// ── Request DTOs (decoupled from Application commands) ────────────────────

public sealed record CreateProfileRequest(
    Guid UserId,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl);
