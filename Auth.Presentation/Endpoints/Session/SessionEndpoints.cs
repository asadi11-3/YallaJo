using Auth.Application.Commands.ForceRevokeUserSessions;
using Auth.Application.Commands.Logout;
using Auth.Application.Commands.LogoutAll;
using Auth.Application.Commands.RevokeSession;
using Auth.Application.Queries.ListSessions;
using Auth.Presentation.Endpoints.Session.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Presentation;

namespace Auth.Presentation.Endpoints.Session;

internal static class SessionEndpoints
{
    internal static void MapSessionEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/logout", async (LogoutRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LogoutCommand(request.RefreshToken), ct);
            return result.ToApiResult();
        })
        .WithName("Logout")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Logout — revokes the refresh token and its session")
        .RequireAuthorization();

        group.MapPost("/logout-all", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LogoutAllCommand(), ct);
            return result.ToApiResult();
        })
        .WithName("LogoutAll")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Logout all sessions — revokes all refresh tokens for the current user")
        .RequireAuthorization();
        group.MapGet("/sessions", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            Guid? currentSessionId = null;
            var sidClaim = currentUser.GetClaim("sid");
            if (sidClaim is not null && Guid.TryParse(sidClaim, out var parsedSid))
                currentSessionId = parsedSid;

            var result = await sender.Send(new ListActiveSessionsQuery(currentUser.UserId.Value), ct);

            var decorated = result.Map(items => (IReadOnlyList<ActiveSessionDto>)items
                .Select(i => new ActiveSessionDto(
                    SessionId: i.SessionId,
                    DeviceId: i.DeviceId,
                    DeviceName: i.DeviceName,
                    UserAgent: i.UserAgent,
                    IpAddress: i.IpAddress,
                    CreatedAt: i.CreatedAt,
                    ExpiresAt: i.ExpiresAt,
                    IsCurrent: currentSessionId.HasValue && i.SessionId == currentSessionId.Value))
                .ToList());

            return decorated.ToApiResult();
        })
        .WithName("ListActiveSessions")
        .Produces<IReadOnlyList<ActiveSessionDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("List all active sessions for the current user")
        .RequireAuthorization();

        group.MapDelete("/sessions/{sessionId:guid}", async (Guid sessionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RevokeSessionCommand(sessionId), ct);
            return result.ToApiResult();
        })
        .WithName("RevokeSession")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Revoke a specific session — user may only revoke their own sessions")
        .RequireAuthorization();

        // Admin endpoint — force-revoke all sessions for a user
        var admin = group.MapGroup("/admin");

        admin.MapDelete("/users/{userId:guid}/sessions", async (Guid userId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ForceRevokeUserSessionsCommand(userId), ct);
            return result.ToApiResult();
        })
        .WithName("ForceRevokeUserSessions")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Admin: force-revoke all sessions and refresh tokens for a user")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();
    }
}
