using ContentTours.Application.Commands.Tour.ToggleTourFeatured;
using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Application.Queries.Tour.GetMyTourStatusCounts;
using ContentTours.Application.Queries.Tour.ListFeaturedTours;
using ContentTours.Application.Queries.Tour.ListMyTours;
using ContentTours.Application.Queries.Tour.SearchTours;
using ContentTours.Application.Queries.Tour.SuggestTours;
using ContentTours.Contracts.Authorization;
using ContentTours.Presentation.Endpoints.TourSearch.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.TourSearch;

internal static class TourSearchEndpoints
{
    internal static void MapTourSearchEndpoints(RouteGroupBuilder group)
    {
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
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = !string.IsNullOrWhiteSpace(lang)
                ? lang
                : http.Request.Headers.AcceptLanguage.ToString();

            var req = new SearchToursRequest(
                q, placeId, priceMin, priceMax, difficulty,
                durationMinutesMin, durationMinutesMax,
                isChildFriendly, isAccessible, isInstantBooking,
                hasDiscount, minRating, acceptLanguage,
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

        group.MapGet("/search/suggest", async (
            string q,
            string? lang,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var acceptLanguage = !string.IsNullOrWhiteSpace(lang)
                ? lang
                : http.Request.Headers.AcceptLanguage.ToString();

            var result = await sender.Send(new SuggestToursQuery(q, acceptLanguage), ct);
            return result.ToApiResult();
        })
        .WithName("SuggestTours")
        .WithSummary("Autocomplete tour name suggestions (matches Tour.Name + TourTranslation.Name)")
        .Produces<IReadOnlyList<TourSuggestDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .AllowAnonymous();

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

        group.MapGet("/provider/my-tours", async (
            int? page, int? pageSize,
            string? status, string? sort,
            Guid? providerUserId,
            bool? includeDeleted,
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            // Admin-tier (Admin / SuperAdmin / Owner) OR explicit ReadAny permission
            // can override providerUserId and view soft-deleted tours.
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                              >= RolePrivilegeLevel.Admin;
            var hasReadAny = currentUser.HasPermission(
                $"ContentTours.Tour.{AppAction.ReadAny}");
            var canOverride = isAdminTier || hasReadAny;

            var effectiveUserId = canOverride && providerUserId.HasValue
                ? providerUserId.Value
                : currentUser.UserId!.Value;

            var query = new ListMyToursQuery(
                effectiveUserId,
                page ?? 1,
                pageSize ?? 20,
                status,
                sort,
                canOverride && (includeDeleted ?? false));

            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        })
        .WithName("ListMyTours")
        .WithSummary("Provider dashboard — list own tours (all statuses)")
        .Produces<ListMyToursResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.ReadOwn));

        // [Backend] B1: per-status counts for the provider "my tours" listing tabs.
        group.MapGet("/provider/my-tours/status-counts", async (
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetMyTourStatusCountsQuery(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyTourStatusCounts")
        .WithSummary("Provider dashboard — per-status counts of own tours")
        .Produces<TourStatusCountsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.ReadOwn));

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
