using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Social.Application.Commands.BanUser;
using Social.Application.Commands.UnbanUser;
using Social.Application.Commands.WarnUser;
using Social.Application.Queries.GetModerationLogs;
using Social.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Social.Presentation.Endpoints;

internal static class ModerationEndpoints
{
    public static RouteGroupBuilder MapModerationEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/v1/moderation/logs
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
        .WithTags("Moderation")
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
        .WithTags("Moderation")
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
        .WithTags("Moderation")
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
        .WithTags("Moderation")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AdminModerationQueue, AppAction.Ban))
        .RequireAuthorization();

        return group;
    }
}

public sealed record WarnUserRequest(
    Guid UserId,
    Social.Domain.Enums.ReportableEntityType EntityType,
    Guid EntityId,
    string Reason)
{
    public WarnUserCommand ToCommand(Guid adminUserId) =>
        new(UserId, EntityType, EntityId, Reason, adminUserId);
}

public sealed record BanUserRequest(
    Guid UserId,
    Social.Domain.Enums.ReportableEntityType EntityType,
    Guid EntityId,
    string Reason,
    DateTime? ExpiresAt)
{
    public BanUserCommand ToCommand(Guid adminUserId) =>
        new(UserId, EntityType, EntityId, Reason, ExpiresAt, adminUserId);
}
