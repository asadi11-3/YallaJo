using ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;
using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatures;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentPlaces.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentPlaces.Presentation.Endpoints.AccessibilityFeature;

internal static class AccessibilityFeatureEndpoints
{
    internal static void MapAccessibilityFeatureEndpoints(RouteGroupBuilder group)
    {
        var accessibility = group.MapGroup("/places")
            .WithTags("ContentPlaces | AccessibilityFeatures");

        accessibility.MapGet("/{id:guid}/accessibility", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAccessibilityFeaturesQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("GetAccessibilityFeatures")
        .Produces<IReadOnlyList<AccessibilityFeatureDto>>(StatusCodes.Status200OK)
        .WithSummary("Get accessibility features for a place")
        .AllowAnonymous();

        accessibility.MapPut("/{id:guid}/accessibility", async (
            Guid id,
            IReadOnlyList<AccessibilityFeatureItemRequest> request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdateAccessibilityFeaturesCommand(id, request), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateAccessibilityFeatures")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Replace all accessibility features for a place (batch replace)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.AccessibilityFeature, AppAction.Update))
        .RequireAuthorization();
    }
}
