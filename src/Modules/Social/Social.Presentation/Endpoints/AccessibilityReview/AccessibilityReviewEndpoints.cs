using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Social.Application.Commands.AccessibilityReview.CreateAccessibilityReview;
using Social.Application.Commands.AccessibilityReview.DeleteAccessibilityReview;
using Social.Application.Commands.AccessibilityReview.UpdateAccessibilityReview;
using Social.Application.Queries.AccessibilityReview.Common;
using Social.Application.Queries.AccessibilityReview.GetMyAccessibilityReviews;
using Social.Application.Queries.AccessibilityReview.GetPublicAccessibilityReviews;
using Social.Contracts.Authorization;
using Social.Domain.Enums;
using Social.Presentation.Endpoints.AccessibilityReview.Models;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Social.Presentation.Endpoints.AccessibilityReview;

internal static class AccessibilityReviewEndpoints
{
    internal static void MapAccessibilityReviewEndpoints(RouteGroupBuilder group)
    {
        group.WithTags("Social | Accessibility Reviews");

        // POST /api/v1/social/accessibility/reviews
        group.MapPost("/", async (
            CreateAccessibilityReviewRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(request.ToCommand(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("CreateAccessibilityReview")
        .WithSummary("Submit a new accessibility-focused review (Phase-3 WS-2)")
        .Accepts<CreateAccessibilityReviewRequest>("application/json")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AccessibilityReview, AppAction.Create))
        .RequireAuthorization();

        // PUT /api/v1/social/accessibility/reviews/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateAccessibilityReviewRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdateAccessibilityReviewCommand(
                    id, currentUser.UserId!.Value,
                    request.Rating, request.Title, request.Content, request.VisitDate, request.FeatureTypesCsv), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateAccessibilityReview")
        .WithSummary("Edit own accessibility review within the 48-hour edit window (S-AR2)")
        .Accepts<UpdateAccessibilityReviewRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AccessibilityReview, AppAction.Update))
        .RequireAuthorization();

        // DELETE /api/v1/social/accessibility/reviews/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var isAdmin = currentUser.HasPermission(
                $"Permission.{SocialFeatures.AdminModerationQueue}.{AppAction.Remove}");
            var result = await sender.Send(
                new DeleteAccessibilityReviewCommand(id, currentUser.UserId!.Value, isAdmin), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteAccessibilityReview")
        .WithSummary("Soft-delete own accessibility review (or admin removal)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AccessibilityReview, AppAction.Delete))
        .RequireAuthorization();

        // GET /api/v1/social/accessibility/reviews?entityType=&entityId=
        group.MapGet("/", async (
            ReviewTargetType entityType,
            Guid entityId,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetPublicAccessibilityReviewsQuery(entityType, entityId,
                    page <= 0 ? 1 : page,
                    pageSize <= 0 ? 20 : pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetPublicAccessibilityReviews")
        .WithSummary("Public: list accessibility reviews for an entity")
        .Produces<PublicAccessibilityReviewPageDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // GET /api/v1/social/accessibility/reviews/my
        group.MapGet("/my", async (
            ICurrentUser currentUser,
            ISender sender,
            string? cursor,
            int pageSize,
            CancellationToken ct) =>
        {
            Guid? afterCursor = Guid.TryParse(cursor, out var g) ? g : null;
            var result = await sender.Send(
                new GetMyAccessibilityReviewsQuery(currentUser.UserId!.Value, afterCursor, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyAccessibilityReviews")
        .WithSummary("Get caller's accessibility reviews (paginated)")
        .Produces<AccessibilityReviewPageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AccessibilityReview, AppAction.Read))
        .RequireAuthorization();
    }
}
