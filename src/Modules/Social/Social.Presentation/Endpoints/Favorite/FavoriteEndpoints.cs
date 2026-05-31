using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Social.Application.Commands.AddFavorite;
using Social.Application.Commands.RemoveFavorite;
using Social.Application.Queries.CheckFavorite;
using Social.Application.Queries.GetMyFavorites;
using Social.Contracts.Authorization;
using Social.Domain.Enums;
using Social.Presentation.Endpoints.Favorite.Models;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Social.Presentation.Endpoints.Favorite;

internal static class FavoriteEndpoints
{
    internal static void MapFavoriteEndpoints(RouteGroupBuilder group)
    {
        group.WithTags("Social | Favorites");

        // POST /api/v1/social/favorites
        group.MapPost("/", async (
            AddFavoriteRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddFavoriteCommand(currentUser.UserId!.Value, request.EntityType, request.EntityId), ct);

            return result.ToApiResult();
        })
        .WithName("AddFavorite")
        .WithSummary("Add a favorite")
        .WithDescription("Adds an entity (Tour, Place, or Business) to the calling user's favorites. Max 500 per user.")
        .Accepts<AddFavoriteRequest>("application/json")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Favorite, AppAction.Create))
        .RequireAuthorization();

        // DELETE /api/v1/social/favorites/{entityType}/{entityId:guid}
        group.MapDelete("/{entityType}/{entityId:guid}", async (
            string entityType,
            Guid entityId,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<FavoriteEntityType>(entityType, ignoreCase: true, out var parsedType))
                return Results.Problem(
                    detail: $"Invalid entity type '{entityType}'. Must be Tour, Place, or Business.",
                    statusCode: StatusCodes.Status400BadRequest);

            var result = await sender.Send(
                new RemoveFavoriteCommand(currentUser.UserId!.Value, parsedType, entityId), ct);

            // S-R7: DELETE is idempotent — always 204 on success (even if not found)
            if (!result.IsSuccess)
                return result.ToApiResult();

            return Results.NoContent();
        })
        .WithName("RemoveFavorite")
        .WithSummary("Remove a favorite")
        .WithDescription("Removes a favorite entry. Idempotent — returns 204 even if the item was not favorited.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Favorite, AppAction.Delete))
        .RequireAuthorization();

        // GET /api/v1/social/favorites
        group.MapGet("/", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] Guid? afterCursor,
            [Microsoft.AspNetCore.Mvc.FromQuery] int pageSize,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetMyFavoritesQuery(currentUser.UserId!.Value, afterCursor, pageSize == 0 ? 20 : pageSize), ct);

            return result.ToApiResult();
        })
        .WithName("GetMyFavorites")
        .WithSummary("Get my favorites")
        .WithDescription("Returns a cursor-paginated list of the calling user's favorites.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Favorite, AppAction.Read))
        .RequireAuthorization();

        // GET /api/v1/social/favorites/check/{entityType}/{entityId:guid}
        group.MapGet("/check/{entityType}/{entityId:guid}", async (
            string entityType,
            Guid entityId,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<FavoriteEntityType>(entityType, ignoreCase: true, out var parsedType))
                return Results.Problem(
                    detail: $"Invalid entity type '{entityType}'. Must be Tour, Place, or Business.",
                    statusCode: StatusCodes.Status400BadRequest);

            var result = await sender.Send(
                new CheckFavoriteQuery(currentUser.UserId!.Value, parsedType, entityId), ct);

            if (!result.IsSuccess)
                return result.ToApiResult();

            return Results.Ok(new { isFavorited = result.Value });
        })
        .WithName("CheckFavorite")
        .WithSummary("Check if an entity is favorited")
        .WithDescription("Returns {isFavorited: bool} for a given entity.")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Favorite, AppAction.Read))
        .RequireAuthorization();
    }
}
