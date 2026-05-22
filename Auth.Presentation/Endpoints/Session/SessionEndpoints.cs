using Auth.Application.Commands.AdminReassignAccount;
using Auth.Application.Commands.AdminResetPassword;
using Auth.Application.Commands.AdminArchiveUser;
using Auth.Application.Commands.AdminReactivateUser;
using Auth.Application.Commands.AdminSuspendUser;
using Auth.Application.Commands.ForceRevokeUserSessions;
using Auth.Application.Commands.Logout;
using Auth.Application.Commands.LogoutAll;
using Auth.Application.Commands.RevokeSession;
using Auth.Application.Queries.ListSessions;
using Auth.Contracts.Authorization;
using Auth.Presentation.Endpoints.Session.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
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
        .WithMetadata(new MustHavePermissionAttribute(AuthFeatures.Session, AppAction.Delete))
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
        .WithMetadata(new MustHavePermissionAttribute(AuthFeatures.Session, AppAction.Delete))
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
        .WithMetadata(new MustHavePermissionAttribute(AuthFeatures.Session, AppAction.Read))
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
        .WithMetadata(new MustHavePermissionAttribute(AuthFeatures.Session, AppAction.Delete))
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
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();

        // Phase 3A — admin-initiated password reset. Lives next to
        // ForceRevokeUserSessions because both are admin-level verbs
        // that tear down a target user's active credentials. The
        // command issues a PasswordResetToken with origin=AdminInitiated,
        // transitions the user to PendingPasswordReset (blocks login),
        // revokes sessions + refresh tokens with reason
        // PasswordResetByAdmin, and queues the reset email via the
        // existing outbox dispatch pipeline.
        admin.MapPost("/users/{userId:guid}/reset-password", async (
            Guid userId,
            AdminResetPasswordRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AdminResetPasswordCommand(userId, request.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("AdminResetPassword")
        .Produces<AdminResetPasswordResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Admin: force a password reset for a user — revokes sessions and sends a reset email.")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();

        admin.MapPatch("/users/{userId:guid}/suspend", async (
            Guid userId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new AdminSuspendUserCommand(userId), ct);
            return result.ToApiResult();
        })
        .WithName("AdminSuspendUser")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Admin: suspend a user account and revoke all active sessions.")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();

        admin.MapPatch("/users/{userId:guid}/reactivate", async (
            Guid userId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new AdminReactivateUserCommand(userId), ct);
            return result.ToApiResult();
        })
        .WithName("AdminReactivateUser")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Admin: reactivate a suspended user account.")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();

        admin.MapPatch("/users/{userId:guid}/archive", async (
            Guid userId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new AdminArchiveUserCommand(userId), ct);
            return result.ToApiResult();
        })
        .WithName("AdminArchiveUser")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Admin: archive a user account and revoke all active sessions.")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();

        // Phase 3C — admin-initiated account reassignment. Retargets
        // the account to a new primary email / new real user, revokes
        // all active sessions and refresh tokens
        // (reason=AccountReassigned), supersedes any outstanding
        // activation/reset tokens, deactivates external-provider
        // links, and issues a fresh activation email to the new
        // address via the existing outbox dispatch pipeline. The old
        // owner loses access immediately: password is replaced with
        // an unusable placeholder and lifecycle moves back to
        // PendingActivation (login gate blocks).
        admin.MapPost("/users/{userId:guid}/reassign", async (
            Guid userId,
            AdminReassignAccountRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AdminReassignAccountCommand(userId, request.NewEmail, request.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("AdminReassignAccount")
        .Produces<AdminReassignAccountResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Admin: reassign a user account to a new email — invalidates credentials, revokes sessions, and sends a fresh activation email.")
        .WithMetadata(new MustHavePermissionAttribute(SecurityFeatures.User, AppAction.UpdateAny))
        .RequireAuthorization();
    }
}
