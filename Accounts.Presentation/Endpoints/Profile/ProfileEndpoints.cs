using Accounts.Application.Commands.DeleteAvatar;
using Accounts.Application.Commands.DeleteProfile;
using Accounts.Application.Commands.RestoreProfile;
using Accounts.Application.Commands.UpdateAvatar;
using Accounts.Application.Commands.UpdateProfile;
using Accounts.Application.Queries.GetProfile;
using Accounts.Presentation.Endpoints.Profile.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Presentation;

namespace Accounts.Presentation.Endpoints.Profile;

internal static class ProfileEndpoints
{
    internal static void MapProfileEndpoints(RouteGroupBuilder group)
    {
        MapSelfServiceEndpoints(group);
        MapProviderEndpoints(group);
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
        .RequireAuthorization();
    }

    // ── Provider Facade: Forwards to Booking Module ──────────────────

    private static void MapProviderEndpoints(RouteGroupBuilder group)
    {
        var provider = group.MapGroup("/provider");

        provider.MapPost("/documents", async (
            [FromForm] Booking.Domain.Enums.DocumentType documentType,
            [FromForm] IFormFile file,
            [FromForm] DateTime? expiresAt,
            ISender sender) =>
        {
            // نمرر الطلب لموديول الحجوزات (Booking) 
            var command = new Booking.Application.Commands.UploadProviderDocument.UploadProviderDocumentCommand(documentType, file, expiresAt);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/v1/booking/provider/documents/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        })
        .WithName("UploadProviderDocumentFacade")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Facade: Upload provider document (forwards to Booking module)")
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}
