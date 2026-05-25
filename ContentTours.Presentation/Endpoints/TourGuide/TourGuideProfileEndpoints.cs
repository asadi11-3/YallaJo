using ContentTours.Application.Commands.TourGuides.AddLanguage;
using ContentTours.Application.Commands.TourGuides.AddSpecialization;
using ContentTours.Application.Commands.TourGuides.DeactivateGuide;
using ContentTours.Application.Commands.TourGuides.RemoveLanguage;
using ContentTours.Application.Commands.TourGuides.UpdateAvatar;
using ContentTours.Application.Commands.TourGuides.UpdateCoverImage;
using ContentTours.Application.Commands.TourGuides.UpdateProfile;
using ContentTours.Application.Queries.TourGuides.Common;
using ContentTours.Application.Queries.TourGuides.GetById;
using ContentTours.Contracts.Authorization;
using ContentTours.Presentation.Endpoints.TourGuide.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.TourGuide;

internal static class TourGuideProfileEndpoints
{
    internal static void MapTourGuideProfileEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTourGuideByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetTourGuideById")
        .WithSummary("Get a public tour guide profile")
        .Produces<TourGuideProfileDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTourGuideProfileRequest request,
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var command = new UpdateTourGuideProfileCommand(
                id,
                currentUser.UserId!.Value,
                request.Bio,
                request.YearsOfExperience,
                request.HasFirstAid,
                request.MoTALicenseNumber);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateTourGuideProfile")
        .WithSummary("Update the authenticated owner's tour guide profile")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update))
        .RequireAuthorization();

        group.MapPost("/{id:guid}/languages", async (
            Guid id,
            AddTourGuideLanguageRequest request,
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var command = new AddTourGuideLanguageCommand(
                id,
                currentUser.UserId!.Value,
                request.LanguageId,
                request.Proficiency);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        })
        .WithName("AddTourGuideLanguage")
        .WithSummary("Add a language to the authenticated owner's tour guide profile")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update))
        .RequireAuthorization();

        group.MapDelete("/{id:guid}/languages/{languageId:guid}", async (
            Guid id,
            Guid languageId,
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var command = new RemoveTourGuideLanguageCommand(
                id,
                currentUser.UserId!.Value,
                languageId);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        })
        .WithName("RemoveTourGuideLanguage")
        .WithSummary("Remove a language from the authenticated owner's tour guide profile")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update))
        .RequireAuthorization();

        group.MapPost("/{id:guid}/specializations", async (
            Guid id,
            AddTourGuideSpecializationRequest request,
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var command = new AddTourGuideSpecializationCommand(
                id,
                currentUser.UserId!.Value,
                request.SpecializationId);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        })
        .WithName("AddTourGuideSpecialization")
        .WithSummary("Add a specialization to the authenticated owner's tour guide profile")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuide, AppAction.Update))
        .RequireAuthorization();

        // GET /guides/me — guide views own profile
        group.MapGet("/me", async (
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTourGuideByIdQuery(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyTourGuideProfile")
        .WithSummary("Get own tour guide profile")
        .Produces<TourGuideProfileDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
        .RequireAuthorization();

        // PUT /guides/me/avatar — guide updates own avatar
        group.MapPut("/me/avatar", async (
            UpdateGuideAvatarRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateGuideAvatarCommand(request.AvatarUrl);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateGuideAvatar")
        .WithSummary("Update own tour guide avatar")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Update))
        .RequireAuthorization();

        // PUT /guides/me/cover-image — guide updates own cover image
        group.MapPut("/me/cover-image", async (
            UpdateGuideCoverImageRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateGuideCoverImageCommand(request.CoverImageUrl);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateGuideCoverImage")
        .WithSummary("Update own tour guide cover image")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Update))
        .RequireAuthorization();

        // DELETE /guides/me — guide self-deactivates
        group.MapDelete("/me", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new DeactivateTourGuideCommand();
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("DeactivateTourGuide")
        .WithSummary("Self-deactivate own tour guide account")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Delete))
        .RequireAuthorization();
    }
}
