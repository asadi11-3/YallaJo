using Accounts.Application.Commands.CreateProfile;
using Accounts.Application.Commands.DeleteAvatar;
using Accounts.Application.Commands.UpdateAvatar;
using Accounts.Application.Commands.UpdateProfile;
using Accounts.Application.Queries.GetProfile;
using Accounts.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
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

        var profile = group.MapGroup("/profile");

        profile.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetProfileQuery(Guid.Empty));
            return ToApiResult(result);
        })
        .WithName("GetProfile")
        .Produces<GetProfileResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get the current user's profile")
        .RequireAuthorization();

        profile.MapPut("/", async (UpdateProfileRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateProfileCommand(
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.Gender,
                request.Country,
                request.City,
                request.AddressLine));
            return ToApiResult(result);
        })
        .WithName("UpdateProfile")
        .Produces<UpdateProfileResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update the current user's profile")
        .RequireAuthorization();

        profile.MapPut("/avatar", async ([FromForm] UpdateAvatarRequest request, ISender sender) =>
        {
            var result = await sender.Send(new UpdateAvatarCommand(request.AvatarUrl));
            return ToApiResult(result);
        })
        .WithName("UpdateAvatar")
        .Accepts<UpdateAvatarRequest>("multipart/form-data")
        .WithMetadata(new ConsumesAttribute("multipart/form-data"))
        .Produces<UpdateAvatarResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update the current user's avatar URL")
        .RequireAuthorization();

        profile.MapDelete("/avatar", async (ISender sender) =>
        {
            var result = await sender.Send(new DeleteAvatarCommand());
            return ToApiResult(result);
        })
        .WithName("DeleteAvatar")
        .Produces<DeleteAvatarResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove the current user's avatar")
        .RequireAuthorization();
    }

    // ── Result → IResult mapping ──────────────────────────────────────────

    private static IResult ToApiResult<T>(Result<T> result, Func<T, string> locationFactory) =>
        result.IsSuccess
            ? Results.Created(locationFactory(result.Value!), result.Value)
            : ToProblem(result.Outcome, result.Errors);

    private static IResult ToApiResult<T>(Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
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

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender? Gender,
    string? Country,
    string? City,
    string? AddressLine);

public sealed record UpdateAvatarRequest(string AvatarUrl);
