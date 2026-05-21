using Accounts.Application.Commands.DeleteAvatar;
using Accounts.Application.Commands.DeleteProfile;
using Accounts.Application.Commands.RestoreProfile;
using Accounts.Application.Commands.UpdateAvatar;
using Accounts.Application.Commands.UpdateProfile;
using Accounts.Application.Queries.GetProfile;
using Accounts.Contracts.Authorization;
using Accounts.Presentation.Endpoints.Profile.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Accounts.Presentation.Endpoints.Profile;

internal static class ProfileEndpoints
{
    internal static void MapProfileEndpoints(RouteGroupBuilder group)
    {
        MapSelfServiceEndpoints(group);
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
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.Profile, AppAction.Read))
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
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.Profile, AppAction.Update))
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

            if (!result.IsSuccess)
            {
                await fileStorage.DeleteAsync(upload.Url, ct);
            }

            return result.ToApiResult();
        })
        .WithName("UpdateAvatar")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<UpdateAvatarResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Upload and set the current user's avatar image")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.Profile, AppAction.Update))
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
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.Profile, AppAction.Update))
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
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.Profile, AppAction.SoftDelete))
        .RequireAuthorization();

        profile.MapPost("/restore", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RestoreProfileCommand(), ct);
            return result.ToApiResult();
        })
        .WithName("RestoreProfile")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Restore the current user's previously soft-deleted profile")
        .WithMetadata(new MustHavePermissionAttribute(AccountsFeatures.Profile, AppAction.Update))
        .RequireAuthorization();
    }
}
