using ContentPlaces.Application.Commands.Place.CreatePlace;
using ContentPlaces.Application.Commands.Place.DeletePlace;
using ContentPlaces.Application.Commands.Place.FeaturePlace;
using ContentPlaces.Application.Commands.Place.UpdatePlace;
using ContentPlaces.Application.Commands.Place.VerifyPlace;
using ContentPlaces.Application.Queries.Place.Common;
using ContentPlaces.Application.Queries.Place.GetPlaceById;
using ContentPlaces.Application.Queries.Place.GetPlaceBySlug;
using ContentPlaces.Application.Queries.Place.ListPlaces;
using ContentPlaces.Presentation.Endpoints.Place.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentPlaces.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation;
using ContentPlaces.Application.Queries.Place.GetMapViewport;
using ContentPlaces.Application.Queries.Place.GetNearbyPlaces;
namespace ContentPlaces.Presentation.Endpoints.Place;

internal static class PlaceEndpoints
{
    internal static void MapPlaceEndpoints(RouteGroupBuilder group)
    {
        var places = group.MapGroup("/places").WithTags("ContentPlaces | Places");

        // ── Anonymous: public reads ───────────────────────────────────────────

        places.MapGet("/", async (
            ISender sender,
            CancellationToken ct,
            int page = 1,
            int pageSize = 20,
            Guid? categoryId = null,
            decimal? ratingMin = null,
            decimal? ratingMax = null,
            string? city = null,
            string? country = null,
            bool? hasActiveTours = null) =>
        {
            var result = await sender.Send(
                new ListPlacesQuery(page, pageSize, categoryId, ratingMin, ratingMax, city, country, hasActiveTours), ct);
            return result.ToApiResult();
        })
        .WithName("ListPlaces")
        .Produces<PaginatedResult<PlaceSummaryDto>>(StatusCodes.Status200OK)
        .WithSummary("List places with pagination and optional filters (max 50 per page)")
        .AllowAnonymous();

        places.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPlaceByIdQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetPlaceById")
        .Produces<PlaceDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get full place details by ID")
        .AllowAnonymous();

        places.MapGet("/{slug}", async (string slug, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPlaceBySlugQuery(slug), ct);
            return result.ToApiResult();
        })
        .WithName("GetPlaceBySlug")
        .Produces<PlaceDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get full place details by slug")
        .AllowAnonymous();

        places.MapGet("/nearby", async (
            ISender sender,
            CancellationToken ct,
            double lat,
            double lng,
            double radiusKm = 10,
            int pageSize = 10) =>
        {
            var result = await sender.Send(new GetNearbyPlacesQuery(lat, lng, radiusKm, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetNearbyPlaces")
        .Produces<IReadOnlyList<NearbyPlaceSummaryDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Get nearby places using Haversine formula (max 100km radius)")
        .AllowAnonymous();

        places.MapGet("/map/viewport", async (
            ISender sender,
            CancellationToken ct,
            double northLat,
            double southLat,
            double eastLng,
            double westLng) =>
        {
            var result = await sender.Send(new GetMapViewportQuery(northLat, southLat, eastLng, westLng), ct);
            return result.ToApiResult();
        })
        .WithName("GetMapViewport")
        .Produces<MapViewportResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Get lightweight map pins for the current viewport bounding box")
        .AllowAnonymous();

        // ── Admin: write operations ───────────────────────────────────────────

        places.MapPost("/", async (CreatePlaceRequest request, ICurrentUser currentUser,
            ISender sender, CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var result = await sender.Send(
                new CreatePlaceCommand(
                request.Name, request.Slug, request.PlaceType,
                request.Latitude, request.Longitude, request.Description,
                request.Address, request.City, request.Country, request.PostalCode,
                request.Phone, request.Email, request.Website,
                request.MetaTitle, request.MetaDescription, currentUser.UserId.Value), ct);

            return result.ToApiResult(r => $"/api/v1/places/{r.PlaceId}");
        })
        .WithName("CreatePlace")
        .Produces<CreatePlaceResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a new place — admin only")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Place, AppAction.Create))
        .RequireAuthorization();

        places.MapPut("/{id:guid}", async (Guid id, UpdatePlaceRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdatePlaceCommand(
                id, request.Name, request.Slug, request.PlaceType,
                request.Latitude, request.Longitude, request.Description,
                request.Address, request.City, request.Country, request.PostalCode,
                request.Phone, request.Email, request.Website,
                request.MetaTitle, request.MetaDescription), ct);
            return result.ToApiResult();
        })
        .WithName("UpdatePlace")
        .Produces<UpdatePlaceResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Update a place — admin only")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Place, AppAction.Update))
        .RequireAuthorization();

        places.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeletePlaceCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("DeletePlace")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Soft-delete a place — blocked if active businesses exist")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Place, AppAction.SoftDelete))
        .RequireAuthorization();

        places.MapPatch("/{id:guid}/feature", async (Guid id, ISender sender, CancellationToken ct,
            bool featured = true) =>
        {
            var result = await sender.Send(new FeaturePlaceCommand(id, featured), ct);
            return result.ToApiResult();
        })
        .WithName("FeaturePlace")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Toggle featured status of a place — admin only")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Place, AppAction.Update))
        .RequireAuthorization();

        places.MapPatch("/{id:guid}/verify", async (Guid id, ISender sender, CancellationToken ct,
            bool verified = true) =>
        {
            var result = await sender.Send(new VerifyPlaceCommand(id, verified), ct);
            return result.ToApiResult();
        })
        .WithName("VerifyPlace")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Toggle verified status of a place — admin only")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Place, AppAction.Update))
        .RequireAuthorization();
    }
}
