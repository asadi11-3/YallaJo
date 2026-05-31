using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Social.Application.Commands.UnbanUser;
using Social.Application.Queries.GetModerationLogs;
using Social.Contracts.Authorization;
using Social.Presentation.Endpoints.Moderation.Models;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Social.Presentation.Endpoints.Moderation;

internal static class ModerationEndpoints
{
    internal static void MapModerationEndpoints(RouteGroupBuilder group)
    {
        group.WithTags("Social | Moderation");

        // GET /api/v1/social/moderation/logs
        group.MapGet("/logs", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] Guid? afterCursor,
            [Microsoft.AspNetCore.Mvc.FromQuery] int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetModerationLogsQuery(afterCursor, pageSize == 0 ? 20 : pageSize), ct);

            return result.ToApiResult();
        })
        .WithName("GetModerationLogs")
        .WithSummary("Admin: content moderation log")
        .WithDescription("Returns a cursor-paginated list of all admin moderation actions (admin-only, immutable audit trail).")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.ContentModerationLog, AppAction.Read))
        .RequireAuthorization();

        // POST /api/v1/social/moderation/warn
        group.MapPost("/warn", async (
            WarnUserRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(request.ToCommand(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("WarnSocialUser")
        .WithSummary("Admin: issue a moderation warning to a user")
        .Accepts<WarnUserRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AdminModerationQueue, AppAction.Warn))
        .RequireAuthorization();

        // POST /api/v1/social/moderation/ban
        group.MapPost("/ban", async (
            BanUserRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(request.ToCommand(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("BanSocialUser")
        .WithSummary("Admin: issue a moderation ban to a user")
        .Accepts<BanUserRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AdminModerationQueue, AppAction.Ban))
        .RequireAuthorization();

        // DELETE /api/v1/social/moderation/ban/{userId}
        group.MapDelete("/ban/{userId:guid}", async (
            Guid userId,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UnbanUserCommand(userId, currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("UnbanSocialUser")
        .WithSummary("Admin: lift an active moderation ban for a user")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AdminModerationQueue, AppAction.Ban))
        .RequireAuthorization();
    }
}
