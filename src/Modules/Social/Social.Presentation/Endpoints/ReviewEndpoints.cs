using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Social.Application.Commands.AddReviewReply;
using Social.Application.Commands.AddHelpfulVote;
using Social.Application.Commands.ApproveReview;
using Social.Application.Commands.CreateReview;
using Social.Application.Commands.DeleteReview;
using Social.Application.Commands.DeleteReviewReply;
using Social.Application.Commands.EditReview;
using Social.Application.Commands.RemoveHelpfulVote;
using Social.Application.Commands.RemoveReview;
using Social.Application.Commands.SubmitReport;
using Social.Application.Commands.UpdateReviewReply;
using Social.Application.Queries.Dtos;
using Social.Application.Queries.GetFlaggedReviews;
using Social.Application.Queries.GetMyReviews;
using Social.Application.Queries.GetPublicReviews;
using Social.Application.Queries.GetRatingSummary;
using Social.Contracts.Authorization;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Social.Presentation.Endpoints;

internal static class ReviewEndpoints
{
    internal static RouteGroupBuilder MapReviewEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/v1/reviews — Create a new review
        group.MapPost("/", async (
            CreateReviewRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(request.ToCommand(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("CreateReview")
        .WithSummary("Create a new review (S-R1/S-R2/S-R4)")
        .WithDescription("Creates a review. Profanity → AwaitingModeration. One review per target.")
        .WithTags("Reviews")
        .Accepts<CreateReviewRequest>("application/json")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Review, AppAction.Create))
        .RequireAuthorization();

        // PUT /api/v1/reviews/{id} — Edit an existing review (S-R3: 48h window)
        group.MapPut("/{id:guid}", async (
            Guid id,
            EditReviewRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(request.ToCommand(id, currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("EditReview")
        .WithSummary("Edit review within 48-hour window (S-R3)")
        .WithTags("Reviews")
        .Accepts<EditReviewRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Review, AppAction.Update))
        .RequireAuthorization();

        // DELETE /api/v1/reviews/{id} — Soft-delete a review
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var isAdmin = currentUser.HasPermission($"Permission.{SocialFeatures.AdminModerationQueue}.{AppAction.Remove}");
            var result = await sender.Send(new DeleteReviewCommand(id, currentUser.UserId!.Value, isAdmin), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteReview")
        .WithSummary("Delete a review (author or admin)")
        .WithTags("Reviews")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Review, AppAction.Delete))
        .RequireAuthorization();

        // POST /api/v1/reviews/{id}/reply — Provider adds reply
        group.MapPost("/{id:guid}/reply", async (
            Guid id,
            AddReplyRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new AddReviewReplyCommand(id, currentUser.UserId!.Value, request.Content), ct);
            return result.ToApiResult();
        })
        .WithName("AddReviewReply")
        .WithSummary("Provider adds a reply to a review")
        .WithTags("Reviews")
        .Accepts<AddReplyRequest>("application/json")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.ReviewReply, AppAction.Create))
        .RequireAuthorization();

        // PUT /api/v1/reviews/{id}/reply/{replyId} — Update a reply
        group.MapPut("/{id:guid}/reply/{replyId:guid}", async (
            Guid id,
            Guid replyId,
            AddReplyRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdateReviewReplyCommand(id, replyId, currentUser.UserId!.Value, request.Content), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateReviewReply")
        .WithSummary("Update an existing reply (S-R3: no time limit for replies)")
        .WithTags("Reviews")
        .Accepts<AddReplyRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.ReviewReply, AppAction.Update))
        .RequireAuthorization();

        // DELETE /api/v1/reviews/{id}/reply/{replyId} — Delete a reply
        group.MapDelete("/{id:guid}/reply/{replyId:guid}", async (
            Guid id,
            Guid replyId,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var isAdmin = currentUser.HasPermission($"Permission.{SocialFeatures.AdminModerationQueue}.{AppAction.Remove}");
            var result = await sender.Send(
                new DeleteReviewReplyCommand(id, replyId, currentUser.UserId!.Value, isAdmin), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteReviewReply")
        .WithSummary("Delete a reply (provider own or admin)")
        .WithTags("Reviews")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.ReviewReply, AppAction.Delete))
        .RequireAuthorization();

        // GET /api/v1/reviews/my-reviews — Paginated list of caller's reviews
        group.MapGet("/my-reviews", async (
            ICurrentUser currentUser,
            ISender sender,
            string? cursor,
            int pageSize,
            CancellationToken ct) =>
        {
            Guid? afterCursor = Guid.TryParse(cursor, out var g) ? g : null;
            var result = await sender.Send(new GetMyReviewsQuery(currentUser.UserId!.Value, afterCursor, pageSize <= 0 ? 20 : pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyReviews")
        .WithSummary("Get caller's reviews (paginated)")
        .WithTags("Reviews")
        .Produces<ReviewPageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Review, AppAction.Read))
        .RequireAuthorization();

        // GET /api/v1/reviews — Public review list for an entity
        group.MapGet("/", async (
            ReviewTargetType entityType,
            Guid entityId,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPublicReviewsQuery(entityType, entityId, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetPublicReviews")
        .WithSummary("Public: list reviews for an entity")
        .WithTags("Reviews")
        .Produces<PublicReviewPageDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // GET /api/v1/social/reviews/{entityType}/{entityId} — Public review list for an entity
        group.MapGet("/{entityType}/{entityId:guid}", async (
            ReviewTargetType entityType,
            Guid entityId,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPublicReviewsQuery(entityType, entityId, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetPublicReviewsByEntity")
        .WithSummary("Public: list approved reviews for an entity")
        .WithTags("Reviews")
        .Produces<PublicReviewPageDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // GET /api/v1/reviews/ratings — Public rating summary for an entity
        group.MapGet("/ratings", async (
            ReviewTargetType entityType,
            Guid entityId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRatingSummaryQuery(entityType, entityId), ct);
            return result.ToApiResult();
        })
        .WithName("GetRatingSummary")
        .WithSummary("Public: get rating summary for an entity")
        .WithTags("Reviews")
        .Produces<RatingSummaryDto>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // POST /api/v1/reviews/{id}/helpful — Helpful vote
        group.MapPost("/{id:guid}/helpful", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new AddHelpfulVoteCommand(id, currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("AddReviewHelpfulVote")
        .WithSummary("Mark a review as helpful")
        .WithTags("Reviews")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Review, AppAction.Vote))
        .RequireAuthorization();

        // DELETE /api/v1/reviews/{id}/helpful — Remove helpful vote
        group.MapDelete("/{id:guid}/helpful", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveHelpfulVoteCommand(id, currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveReviewHelpfulVote")
        .WithSummary("Remove helpful vote from a review")
        .WithTags("Reviews")
        .Produces(StatusCodes.Status200OK)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Review, AppAction.Vote))
        .RequireAuthorization();

        // GET /api/v1/reviews/admin/flagged — Admin: list flagged/awaiting-moderation reviews
        group.MapGet("/admin/flagged", async (
            ISender sender,
            string? cursor,
            int pageSize,
            CancellationToken ct) =>
        {
            Guid? afterCursor = Guid.TryParse(cursor, out var g) ? g : null;
            var result = await sender.Send(new GetFlaggedReviewsQuery(afterCursor, pageSize <= 0 ? 20 : pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetFlaggedReviews")
        .WithSummary("Admin: get flagged/awaiting-moderation reviews (S-R4/S-R5)")
        .WithTags("Reviews")
        .Produces<ReviewPageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AdminModerationQueue, AppAction.Read))
        .RequireAuthorization();

        return group;
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

internal sealed record CreateReviewRequest(
    ReviewTargetType TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate)
{
    internal CreateReviewCommand ToCommand(Guid userId) =>
        new(userId, TargetType, TargetId, Rating, Title, Content, VisitDate);
}

internal sealed record EditReviewRequest(
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate)
{
    internal EditReviewCommand ToCommand(Guid reviewId, Guid userId) =>
        new(reviewId, userId, Rating, Title, Content, VisitDate);
}

internal sealed record AddReplyRequest(string Content);

// ── T4 Admin routes on the /api/v1/reviews group ──────────────────────────────
internal static class ReviewAdminEndpoints
{
    public static RouteGroupBuilder MapReviewAdminEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/v1/reviews/{id}/report — shorthand: report a specific review
        group.MapPost("/{id:guid}/report", async (
            Guid id,
            ReviewReportRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new SubmitReportCommand(
                    currentUser.UserId!.Value,
                    ReportableEntityType.Review,
                    id,
                    request.Reason,
                    request.Description), ct);

            return result.ToApiResult();
        })
        .WithName("ReportReview")
        .WithSummary("Report a review")
        .WithDescription("Shorthand to report a specific review for moderation.")
        .WithTags("Reviews")
        .Accepts<ReviewReportRequest>("application/json")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Report, AppAction.Create))
        .RequireAuthorization();

        // POST /api/v1/reviews/admin/{id}/approve — admin approves a flagged review
        group.MapPost("/admin/{id:guid}/approve", async (
            Guid id,
            AdminReviewNotesRequest? request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ApproveReviewCommand(currentUser.UserId!.Value, id, request?.Notes), ct);

            return result.ToApiResult();
        })
        .WithName("ApproveReview")
        .WithSummary("Admin: approve a review")
        .WithDescription("Restores a flagged or auto-hidden review to Published status.")
        .WithTags("Reviews")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AdminModerationQueue, AppAction.Approve))
        .RequireAuthorization();

        // POST /api/v1/reviews/admin/{id}/remove — admin removes a review
        group.MapPost("/admin/{id:guid}/remove", async (
            Guid id,
            AdminReviewNotesRequest? request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new RemoveReviewCommand(currentUser.UserId!.Value, id, request?.Notes), ct);

            return result.ToApiResult();
        })
        .WithName("RemoveReviewByAdmin")
        .WithSummary("Admin: remove a review")
        .WithDescription("Admin permanently removes (status=RemovedByAdmin) a review and logs the action.")
        .WithTags("Reviews")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AdminModerationQueue, AppAction.Remove))
        .RequireAuthorization();

        return group;
    }
}

internal sealed record ReviewReportRequest(ReportReason Reason, string Description);
internal sealed record AdminReviewNotesRequest(string? Notes = null);
