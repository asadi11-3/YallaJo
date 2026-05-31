using ContentTours.Application.Commands.GuideAvailabilityBlock.Create;
using ContentTours.Application.Commands.GuideAvailabilityBlock.Delete;
using ContentTours.Application.Commands.TourGuides.AddLanguage;
using ContentTours.Application.Commands.TourGuides.AddSpecialization;
using ContentTours.Application.Commands.TourGuides.DeactivateGuide;
using ContentTours.Application.Commands.TourGuides.RemoveLanguage;
using ContentTours.Application.Commands.TourGuides.UpdateAvatar;
using ContentTours.Application.Commands.TourGuides.UpdateCoverImage;
using ContentTours.Application.Commands.TourGuides.UpdateProfile;
using ContentTours.Application.Queries.GuideAvailabilityBlock;
using ContentTours.Application.Queries.TourGuide.Analytics;
using ContentTours.Application.Queries.TourGuide.Earnings;
using ContentTours.Application.Queries.TourGuide.GetTierProgress;
using ContentTours.Application.Queries.TourGuides.Common;
using ContentTours.Application.Queries.TourGuides.GetById;
using ContentTours.Application.Queries.TourGuides.GetByUserId;
using ContentTours.Application.Queries.TourGuides.GetBySlug;
using ContentTours.Application.Queries.TourGuides.GetGuideTours;
using ContentTours.Application.Queries.TourGuides.GetMyApplications;
using ContentTours.Application.Queries.TourGuides.ListPublic;
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
        // GET /guides — public list of active tour guides
        group.MapGet("/", async (
            int page,
            int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListTourGuidesQuery(page, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("ListPublicTourGuides")
        .WithSummary("List active tour guides (public)")
        .Produces<ListTourGuidesResult>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // GET /guides/by-slug/{slug} — public guide lookup by slug
        group.MapGet("/by-slug/{slug}", async (
            string slug,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTourGuideBySlugQuery(slug), ct);
            return result.ToApiResult();
        })
        .WithName("GetTourGuideBySlug")
        .WithSummary("Get a tour guide profile by slug (public)")
        .Produces<TourGuideProfileDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // GET /guides/me/applications — guide's own applications
        group.MapGet("/me/applications", async (
            int page,
            int pageSize,
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetMyGuideApplicationsQuery(currentUser.UserId!.Value, page, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyGuideApplications")
        .WithSummary("List own guide applications")
        .Produces<GetMyGuideApplicationsResult>(StatusCodes.Status200OK)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
        .RequireAuthorization();

        // GET /guides/{id}/tours — guide's assigned tours
        group.MapGet("/{id:guid}/tours", async (
            Guid id,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGuideToursQuery(id, page, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetGuideTours")
        .WithSummary("Get tours assigned to a guide")
        .Produces<GetGuideToursResult>(StatusCodes.Status200OK)
        .AllowAnonymous();

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
            // F15 fix: was GetTourGuideByIdQuery — that query filters by aggregate Id, but
    // currentUser.UserId is the OWNING user identity, not the aggregate Id. The
    // mismatch made every /guides/me call 404 even when the TourGuide row existed.
    var result = await sender.Send(new GetTourGuideByUserIdQuery(currentUser.UserId!.Value), ct);
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
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.DeleteOwn))
        .RequireAuthorization();

        // GET /guides/me/availability-blocks
        group.MapGet("/me/availability-blocks", async (ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
            (await sender.Send(new GetMyAvailabilityBlocksQuery(currentUser.UserId!.Value), ct)).ToApiResult())
            .WithName("GetMyAvailabilityBlocks")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
            .RequireAuthorization();

        // POST /guides/me/availability-blocks
        group.MapPost("/me/availability-blocks", async (CreateGuideAvailabilityBlockRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(request.ToCommand(), ct)).ToApiResult())
            .WithName("CreateAvailabilityBlock")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Update))
            .RequireAuthorization();

        // DELETE /guides/me/availability-blocks/{id}
        group.MapDelete("/me/availability-blocks/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeleteGuideAvailabilityBlockCommand(id), ct)).ToApiResult())
            .WithName("DeleteAvailabilityBlock")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Update))
            .RequireAuthorization();

        // GET /guides/me/tier
        group.MapGet("/me/tier", async (ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
            (await sender.Send(new GetGuideTierProgressQuery(currentUser.UserId!.Value), ct)).ToApiResult())
            .WithName("GetGuideTierProgress")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
            .RequireAuthorization();

        // GET /guides/me/earnings/summary
        group.MapGet("/me/earnings/summary", async (ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
            (await sender.Send(new GetGuideEarningsSummaryQuery(currentUser.UserId!.Value), ct)).ToApiResult())
            .WithName("GetMyTourGuideEarningsSummary")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
            .RequireAuthorization();

        // GET /guides/me/earnings/by-tour
        group.MapGet("/me/earnings/by-tour", async (ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
            (await sender.Send(new GetGuideEarningsByTourQuery(currentUser.UserId!.Value), ct)).ToApiResult())
            .WithName("GetGuideEarningsByTour")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
            .RequireAuthorization();

        // GET /guides/me/earnings/history
        group.MapGet("/me/earnings/history", async ([AsParameters] GuideDashboardPageRequest request, ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
            (await sender.Send(new GetGuideEarningsHistoryQuery(currentUser.UserId!.Value, request.Page, request.PageSize), ct)).ToApiResult())
            .WithName("GetGuideEarningsHistory")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/me/analytics/overview", async (ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
            (await sender.Send(new GetGuideBookingOverviewQuery(currentUser.UserId!.Value), ct)).ToApiResult())
            .WithName("GetGuideAnalyticsOverview")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/me/analytics/booking-trends", async ([AsParameters] GuideBookingTrendsRequest request, ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
            (await sender.Send(new GetGuideBookingTrendsQuery(currentUser.UserId!.Value, request.Granularity, request.Months), ct)).ToApiResult())
            .WithName("GetGuideBookingTrends")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/me/analytics/popular-tours", async ([AsParameters] GuidePopularToursRequest request, ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
            (await sender.Send(new GetGuidePopularToursQuery(currentUser.UserId!.Value, request.Limit), ct)).ToApiResult())
            .WithName("GetGuidePopularTours")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/me/analytics/peak-days", async (ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
            (await sender.Send(new GetGuidePeakDaysQuery(currentUser.UserId!.Value), ct)).ToApiResult())
            .WithName("GetGuidePeakDays")
            .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.TourGuideProfile, AppAction.Read))
            .RequireAuthorization();
    }
}
