using ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;
using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatures;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Presentation;

namespace ContentPlaces.Presentation.Endpoints.AccessibilityFeature;

internal static class AccessibilityFeatureEndpoints
{
    private const string UpdatePermission = "Permission.AccessibilityFeature.Update";

    internal static void MapAccessibilityFeatureEndpoints(RouteGroupBuilder group)
    {
        var accessibility = group.MapGroup("/places")
            .WithTags("ContentPlaces | AccessibilityFeature");

        // GET
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
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Get accessibility features")
        .WithDescription("Returns accessibility features for a place")
        .AllowAnonymous();

        // PUT (replace all)
        accessibility.MapPut("/{id:guid}/accessibility", async (
            Guid id,
            IReadOnlyList<AccessibilityFeatureItemRequest> request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new UpdateAccessibilityFeaturesCommand(id, request);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateAccessibilityFeatures")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update accessibility features")
        .WithDescription("Replaces all accessibility features for a place (batch replace)")
        .RequireAuthorization(UpdatePermission);
    }
}
