using Auth.Application.Commands.AcceptInvite;
using Auth.Application.Commands.InviteUser;
using Auth.Application.Commands.ResendInvite;
using Auth.Presentation.Endpoints.Invitation.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;

namespace Auth.Presentation.Endpoints.Invitation;

internal static class InvitationEndpoints
{
    internal static void MapInvitationEndpoints(RouteGroupBuilder group)
    {
        MapInviteUserEndpoint(group);
        MapAcceptInviteEndpoint(group);
        MapResendInviteEndpoint(group);
    }

    // ── Admin: invite a user ─────────────────────────────────────────────────

    private static void MapInviteUserEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/invitations", async (
                InviteUserRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new InviteUserCommand(
                        request.Email,
                        request.FirstName,
                        request.LastName,
                        request.DisplayName,
                        request.AvatarUrl), ct);

                return result
                    .Map(r => new InviteUserResponse(
                        r.UserId,
                        r.ProfileId,
                        "Invite sent. The user will receive an email with an acceptance link."))
                    .ToApiResult();
            })
            .WithName("InviteUser")
            .Produces<InviteUserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Admin: invite a new user — creates identity (pending) + profile and sends invite email.")
            .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Create))
            .RequireAuthorization();
    }

    // ── Anonymous: accept an invite ──────────────────────────────────────────

    private static void MapAcceptInviteEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/invitations/accept", async (
                AcceptInviteRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AcceptInviteCommand(
                        request.Email,
                        request.Token,
                        request.Password,
                        request.ConfirmPassword), ct);

                return result
                    .Map(r => new AcceptInviteResponse(
                        r.UserId,
                        "Invite accepted. Your account is now active — please sign in."))
                    .ToApiResult();
            })
            .WithName("AcceptInvite")
            .Produces<AcceptInviteResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithSummary("Accept an invite — set password, verify email, activate account.")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.OtpPolicy);
    }

    // ── Admin: resend invite ─────────────────────────────────────────────────

    private static void MapResendInviteEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/invitations/resend", async (
                ResendInviteRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new ResendInviteCommand(request.Email), ct);
                return result.ToApiResult();
            })
            .WithName("ResendInvite")
            .Produces<ResendInviteResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Admin: resend an invite link for a pending account.")
            .WithMetadata(new MustHavePermissionAttribute(AppFeatures.User, AppAction.Create))
            .RequireAuthorization();
    }
}
