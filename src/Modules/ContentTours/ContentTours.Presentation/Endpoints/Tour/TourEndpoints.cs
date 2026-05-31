using ContentTours.Application.Commands.Tour.ArchiveTour;
using ContentTours.Application.Commands.Tour.ApproveTour;
using ContentTours.Application.Commands.Tour.CreateTour;
using ContentTours.Application.Commands.Tour.DeleteTour;
using ContentTours.Application.Commands.Tour.ReinstateTour;
using ContentTours.Application.Commands.Tour.RejectTour;
using ContentTours.Application.Commands.Tour.SubmitTour;
using ContentTours.Application.Commands.Tour.SuspendTour;
using ContentTours.Application.Commands.Tour.UpdateTour;
using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Application.Queries.Tour.GetTourById;
using ContentTours.Application.Queries.Tour.GetTourBySlug;
using ContentTours.Application.Queries.Tour.ListTours;
using ContentTours.Contracts.Authorization;
using ContentTours.Presentation.Endpoints.Tour.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.Tour;

internal static class TourEndpoints
{
    internal static void MapTourEndpoints(RouteGroupBuilder group)
    {
        // ── GET /api/v1/tours ─────────────────────────────────────────────────
        group.MapGet("/", async (
            int? page,
            int? pageSize,
            string? sort,
            Guid? placeId,
            bool? isFeatured,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();

            var result = await sender.Send(
                new ListToursQuery(page ?? 1, pageSize ?? 20, sort, placeId, isFeatured, acceptLanguage),
                ct);

            return result.ToApiResult();
        })
        .WithName("ListTours")
        .WithSummary("List public tours with pagination and optional filters")
        .Produces<PaginatedResult<TourSummaryDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .AllowAnonymous();

        // ── GET /api/v1/tours/{id} ────────────────────────────────────────────
        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext http,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();

            // F45 2026-05-30: pass the caller's identity so the owner or an admin can
            // read a Draft/non-Approved tour; anonymous callers stay on the public
            // (Approved-only) path. Endpoint remains AllowAnonymous for public reads.
            var isPrivileged = currentUser.IsInRole("Admin")
                || currentUser.IsInRole("SuperAdmin")
                || currentUser.IsInRole("Owner");

            var result = await sender.Send(
                new GetTourByIdQuery(id, acceptLanguage, currentUser.UserId, isPrivileged),
                ct);
            return result.ToApiResult();
        })
        .WithName("GetTourById")
        .WithSummary("Get tour details by ID (owner/admin may view non-public statuses)")
        .Produces<TourDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // ── GET /api/v1/tours/slug/{slug} ─────────────────────────────────────
        group.MapGet("/slug/{slug}", async (
            string slug,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();

            var result = await sender.Send(new GetTourBySlugQuery(slug, acceptLanguage), ct);
            return result.ToApiResult();
        })
        .WithName("GetTourBySlug")
        .WithSummary("Get public tour details by slug")
        .Produces<TourDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // ── GET /api/v1/tours/by-slug/{slug} ──────────────────────────────────
        // F20 2026-05-29: standardize on the `/by-slug/` convention used by
        // /api/v1/guides/by-slug/{slug} (TourGuideProfileEndpoints.cs:54).
        // Kept `/slug/` above for backward compatibility.
        group.MapGet("/by-slug/{slug}", async (
            string slug,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = http.Request.Headers.AcceptLanguage.ToString();

            var result = await sender.Send(new GetTourBySlugQuery(slug, acceptLanguage), ct);
            return result.ToApiResult();
        })
        .WithName("GetTourByCanonicalSlug")
        .WithSummary("Get public tour details by slug (canonical path)")
        .Produces<TourDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // ── POST /api/v1/tours ────────────────────────────────────────────────
        group.MapPost("/", async (
            CreateTourRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateTourCommand(
                request.Name,
                request.Slug,
                request.Difficulty,
                request.DurationMinutes,
                request.MaxGroupSize,
                request.BasePrice,
                request.Currency,
                request.Latitude,
                request.Longitude,
                request.Description,
                request.ShortDescription,
                request.MinAge,
                request.MeetingPointLatitude,
                request.MeetingPointLongitude,
                request.PlaceId ?? Guid.Empty,
                request.IsChildFriendly,
                request.IsAccessible,
                request.AgeRestriction,
                request.IsInstantBooking,
                request.CancellationPolicyHours,
                request.MetaTitle,
                request.MetaDescription);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult(r => $"/api/v1/tours/{r.TourId}");
        })
        .WithName("CreateTour")
        .WithSummary("Create a new tour in Draft status")
        .Produces<CreateTourResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Create));

        // ── PUT /api/v1/tours/{id} ────────────────────────────────────────────
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTourRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateTourCommand(
                id,
                request.RowVersion,
                request.Name,
                request.Slug,
                request.Difficulty,
                request.DurationMinutes,
                request.MaxGroupSize,
                request.BasePrice,
                request.Currency,
                request.Latitude,
                request.Longitude,
                request.Description,
                request.ShortDescription,
                request.MinAge,
                request.MeetingPointLatitude,
                request.MeetingPointLongitude,
                request.PlaceId ?? Guid.Empty,
                request.IsChildFriendly,
                request.IsAccessible,
                request.AgeRestriction,
                request.IsInstantBooking,
                request.CancellationPolicyHours,
                request.MetaTitle,
                request.MetaDescription);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateTour")
        .WithSummary("Update a draft or rejected tour")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Update));

        // ── DELETE /api/v1/tours/{id} ─────────────────────────────────────────
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteTourCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteTour")
        .WithSummary("Soft-delete a tour (owner; Admin+ via override)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.DeleteOwn));

        // ── POST /api/v1/tours/{id}/submit ────────────────────────────────────
        group.MapPost("/{id:guid}/submit", async (
            Guid id,
            RowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitTourCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("SubmitTour")
        .WithSummary("Submit a draft tour for admin review")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Submit));

        // ── POST /api/v1/tours/admin/{id}/approve ─────────────────────────────
        group.MapPost("/admin/{id:guid}/approve", async (
            Guid id,
            RowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ApproveTourCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("ApproveTour")
        .WithSummary("Admin: approve a pending tour")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Approve));

        // ── POST /api/v1/tours/admin/{id}/reject ──────────────────────────────
        group.MapPost("/admin/{id:guid}/reject", async (
            Guid id,
            RejectTourRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new RejectTourCommand(id, request.RowVersion, request.Reason),
                ct);
            return result.ToApiResult();
        })
        .WithName("RejectTour")
        .WithSummary("Admin: reject a pending tour")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Reject));

        // ── POST /api/v1/tours/admin/{id}/suspend ─────────────────────────────
        group.MapPost("/admin/{id:guid}/suspend", async (
            Guid id,
            SuspendTourRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new SuspendTourCommand(id, request.RowVersion, request.Reason),
                ct);
            return result.ToApiResult();
        })
        .WithName("SuspendTour")
        .WithSummary("Admin: suspend an approved tour")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Suspend));

        // ── POST /api/v1/tours/admin/{id}/reinstate ───────────────────────────
        group.MapPost("/admin/{id:guid}/reinstate", async (
            Guid id,
            RowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ReinstateTourCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("ReinstateTour")
        .WithSummary("Admin: reinstate a suspended tour")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Reinstate));

        // ── POST /api/v1/tours/{id}/archive ───────────────────────────────────
        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            RowVersionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ArchiveTourCommand(id, request.RowVersion), ct);
            return result.ToApiResult();
        })
        .WithName("ArchiveTour")
        .WithSummary("Provider: archive own tour")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Archive));
    }
}
