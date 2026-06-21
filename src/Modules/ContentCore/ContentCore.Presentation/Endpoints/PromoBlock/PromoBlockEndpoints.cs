using ContentCore.Application.Commands.PromoBlock.SetPromoBlockImage;
using ContentCore.Application.Commands.PromoBlock.UpdatePromoBlock;
using ContentCore.Application.Queries.PromoBlock.Common;
using ContentCore.Application.Queries.PromoBlock.ListPromoBlocksAdmin;
using ContentCore.Application.Queries.PromoBlock.ListPromoBlocksByKeys;
using ContentCore.Contracts.Authorization;
using ContentCore.Presentation.Endpoints.PromoBlock.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentCore.Presentation.Endpoints.PromoBlock;

internal static class PromoBlockEndpoints
{
    internal static void MapPromoBlockEndpoints(RouteGroupBuilder group)
    {
        var promos = group.MapGroup("/promo-blocks").WithTags("ContentCore | PromoBlocks");

        // Public: active + in-window promo blocks only.
        promos.MapGet("/", async (string? keys, ISender sender, CancellationToken ct) =>
        {
            var keyList = ParseKeys(keys);
            var result = await sender.Send(new ListPromoBlocksByKeysQuery(keyList), ct);
            return result.ToApiResult();
        })
            .WithName("ListPromoBlocks")
            .Produces<IReadOnlyList<PromoBlockDto>>(StatusCodes.Status200OK)
            .WithSummary("List active promo blocks for the supplied placement keys.")
            .AllowAnonymous();

        // Admin: includes inactive placements for inline editing.
        promos.MapGet("/admin", async (string? keys, ISender sender, CancellationToken ct) =>
        {
            var keyList = ParseKeys(keys);
            var result = await sender.Send(new ListPromoBlocksAdminQuery(keyList), ct);
            return result.ToApiResult();
        })
            .WithName("ListPromoBlocksAdmin")
            .Produces<IReadOnlyList<PromoBlockDto>>(StatusCodes.Status200OK)
            .WithSummary("List all promo blocks (including inactive) for the supplied placement keys.")
            .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Promotion, AppAction.Read))
            .RequireAuthorization();

        // Admin: update promo block content.
        promos.MapPut("/{key}", async (string key, UpdatePromoBlockRequest request, ISender sender, CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new UpdatePromoBlockCommand(
                    key,
                    request.Title,
                    request.Description,
                    request.ButtonText,
                    request.ButtonUrl,
                    request.BadgeText,
                    request.IconName,
                    request.IsActive,
                    request.SortOrder,
                    request.StartsAt,
                    request.EndsAt),
                ct);
            return result.ToApiResult();
        })
            .WithName("UpdatePromoBlock")
            .Produces<UpdatePromoBlockResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Update the content of a promo block placement.")
            .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Promotion, AppAction.Update))
            .RequireAuthorization();

        // Admin: upload/replace promo block image.
        promos.MapPost("/{key}/image", async (string key, IFormFile file, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            if (currentUser.UserId is null)
            {
                return Results.Unauthorized();
            }

            if (file is null || file.Length == 0)
            {
                return Results.BadRequest("An image file is required.");
            }

            await using var bufferedStream = new MemoryStream(checked((int)file.Length));
            await file.CopyToAsync(bufferedStream, ct);
            bufferedStream.Position = 0;

            var result = await sender.Send(
                new SetPromoBlockImageCommand(key, bufferedStream, file.FileName, file.ContentType, bufferedStream.Length),
                ct);
            return result.ToApiResult();
        })
            .WithName("SetPromoBlockImage")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<SetPromoBlockImageResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Upload or replace the image for a promo block placement.")
            .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Promotion, AppAction.Update))
            .RequireAuthorization()
            .DisableAntiforgery();
    }

    private static string[] ParseKeys(string? keys) =>
        string.IsNullOrWhiteSpace(keys)
            ? []
            : keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
