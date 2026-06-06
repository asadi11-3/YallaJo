using ContentPlaces.Application.Commands.AccessibilityFeature.DeleteAccessibilityFeatureAssignment;
using ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;
using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatureCatalog;
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

        // WS-1 (Phase 3 G2): public catalog of accessibility feature types — feeds admin pickers + filter chips.
        accessibility.MapGet("/accessibility/catalog", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAccessibilityFeatureCatalogQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetAccessibilityFeatureCatalog")
        .Produces<IReadOnlyList<AccessibilityFeatureCatalogItem>>(StatusCodes.Status200OK)
        .WithSummary("List all accessibility feature types (Wheelchair/Visual/Hearing/Cognitive/Mobility/Other)")
        .AllowAnonymous();

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

        // WS-1 (Phase 3 G2): admin deletes a single accessibility-feature assignment row (place- or business-level).
        accessibility.MapDelete("/admin/accessibility/{assignmentId:guid}", async (
            Guid assignmentId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new DeleteAccessibilityFeatureAssignmentCommand(assignmentId), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteAccessibilityFeatureAssignment")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Admin: delete a single accessibility-feature assignment row")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.AccessibilityFeature, AppAction.Delete))
        .RequireAuthorization();
    }
}
