using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

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

        return group;
    }
}
