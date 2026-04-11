using Accounts.Application.Commands.CreateProfile;
using Accounts.Application.Commands.DeleteAvatar;
using Accounts.Application.Commands.DeleteProfile;
using Accounts.Application.Commands.UpdateAvatar;
using Accounts.Application.Commands.UpdateProfile;
using Accounts.Application.Queries.GetProfile;
using Accounts.Presentation.Endpoints.Profile.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Presentation;

namespace Accounts.Presentation.Endpoints.Profile;

internal static class ProfileEndpoints
{
    internal static void MapProfileEndpoints(RouteGroupBuilder group)
    {
        MapAdminEndpoints(group);
        MapSelfServiceEndpoints(group);
    }

    // ── Admin: collection-scoped operations (/profiles) ───────────────────────

    private static void MapAdminEndpoints(RouteGroupBuilder group)
    {
        var profiles = group.MapGroup("/profiles");

        profiles.MapPost("/", async (CreateProfileRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new CreateProfileCommand(
                request.UserId,
                request.FirstName,
                request.LastName,
                request.DisplayName,
                request.AvatarUrl), ct);

            return result.ToApiResult(id => $"/api/v1/accounts/profiles/{id}");
        })
        .WithName("CreateProfile")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a user profile linked to an existing security user")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Create))
        .RequireAuthorization();
    }

    // ── Self-service: current user's own profile (/profile) ──────────────────

    private static void MapSelfServiceEndpoints(RouteGroupBuilder group)
    {
        var profile = group.MapGroup("/profile");

       
        profile.MapGet("/", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var result = await sender.Send(new GetProfileQuery(currentUser.UserId.Value), ct);
            return result.ToApiResult();
        })
        .WithName("GetProfile")
        .Produces<GetProfileResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get the current user's profile")
        .RequireAuthorization();

        profile.MapPut("/", async (UpdateProfileRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdateProfileCommand(
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.Gender,
                request.Country,
                request.City,
                request.AddressLine), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateProfile")
        .Produces<UpdateProfileResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update the current user's profile")
        .RequireAuthorization();

        profile.MapPut("/avatar", async (
            IFormFile file,
            ISender sender,
            IFileStorageService fileStorage,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
            {
                return Results.ValidationProblem(
                   new Dictionary<string, string[]> { { "file", ["An image file is required."] } });
            }

            await using var stream = file.OpenReadStream();
            var upload = await fileStorage.UploadAsync(
                stream, file.FileName, file.ContentType, "avatars", ct);

            var result = await sender.Send(new UpdateAvatarCommand(upload.Url), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateAvatar")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<UpdateAvatarResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Upload and set the current user's avatar image")
        .RequireAuthorization()
        .DisableAntiforgery();

        profile.MapDelete("/avatar", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteAvatarCommand(), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteAvatar")
        .Produces<DeleteAvatarResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove the current user's avatar")
        .RequireAuthorization();

        profile.MapDelete("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteProfileCommand(), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteProfile")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete the current user's profile")
        .RequireAuthorization();
    }
}
