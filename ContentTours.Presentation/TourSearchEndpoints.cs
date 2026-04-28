using ContentTours.Application.Commands.Tour.ToggleTourFeatured;
using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Application.Queries.Tour.ListFeaturedTours;
using ContentTours.Application.Queries.Tour.ListMyTours;
using ContentTours.Application.Queries.Tour.SearchTours;
using ContentTours.Application.Queries.Tour.SuggestTours;
using ContentTours.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation;

public static class TourSearchEndpoints
{
    public static void MapTourSearchEndpoints(RouteGroupBuilder group)
    {
        // ── GET /api/v1/tours/search ──────────────────────────────────────────
        group.MapGet("/search", async (
            string? q,
            Guid? placeId,
            decimal? priceMin, decimal? priceMax,
            string? difficulty,
            int? durationMinutesMin, int? durationMinutesMax,
            bool? isChildFriendly, bool? isAccessible, bool? isInstantBooking,
            bool? hasDiscount,
            decimal? minRating,
            string? lang,
            SearchSort? sort,
            int? page, int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var req = new SearchToursRequest(
                q, placeId, priceMin, priceMax, difficulty,
                durationMinutesMin, durationMinutesMax,
                isChildFriendly, isAccessible, isInstantBooking,
                hasDiscount, minRating, lang ?? "en",
                sort ?? SearchSort.Relevance,
                page ?? 1, pageSize ?? 20);
            var result = await sender.Send(new SearchToursQuery(req), ct);
            return result.ToApiResult();
        })
        .WithName("SearchTours")
        .WithSummary("Full-text + faceted tour search")
        .Produces<SearchToursResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .AllowAnonymous();

        // ── GET /api/v1/tours/search/suggest ─────────────────────────────────
        group.MapGet("/search/suggest", async (
            string q,
            string? lang,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SuggestToursQuery(q, lang ?? "en"), ct);
            return result.ToApiResult();
        })
        .WithName("SuggestTours")
        .WithSummary("Autocomplete tour name suggestions")
        .Produces<IReadOnlyList<TourSuggestDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .AllowAnonymous();

        // ── GET /api/v1/tours/featured ────────────────────────────────────────
        group.MapGet("/featured", async (
            string? lang,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListFeaturedToursQuery(lang ?? "en"), ct);
            return result.ToApiResult();
        })
        .WithName("ListFeaturedTours")
        .WithSummary("List editorially curated featured tours (top 20)")
        .Produces<IReadOnlyList<TourSummaryDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // ── GET /api/v1/tours/provider/my-tours ───────────────────────────────
        group.MapGet("/provider/my-tours", async (
            int? page, int? pageSize,
            string? status, string? sort,
            Guid? providerUserId,
            bool? includeDeleted,
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            // IDOR prevention: non-admins always see their own tours
            var isAdmin = currentUser.IsInRole("Admin") ||
                          currentUser.HasPermission($"ContentTours.Tour.{AppAction.ReadAny}");
            var effectiveUserId = isAdmin && providerUserId.HasValue
                ? providerUserId.Value
                : currentUser.UserId!.Value;

            var query = new ListMyToursQuery(
                effectiveUserId,
                page ?? 1,
                pageSize ?? 20,
                status,
                sort,
                isAdmin && (includeDeleted ?? false));

            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("ListMyTours")
        .WithSummary("Provider dashboard — list own tours (all statuses)")
        .Produces<ListMyToursResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.ReadOwn));

        // ── PATCH /api/v1/tours/admin/{id}/feature ────────────────────────────
        group.MapPatch("/admin/{id:guid}/feature", async (
            Guid id,
            ToggleTourFeaturedRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ToggleTourFeaturedCommand(id, request.IsFeatured), ct);
            return result.ToApiResult();
        })
        .WithName("ToggleTourFeatured")
        .WithSummary("Admin: feature or unfeature a tour")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Feature));
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record ToggleTourFeaturedRequest(bool IsFeatured);
