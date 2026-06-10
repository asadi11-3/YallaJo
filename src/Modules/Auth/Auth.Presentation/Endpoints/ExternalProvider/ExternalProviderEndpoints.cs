using Auth.Application.Commands.ExternalLogin;
using Auth.Application.Commands.LinkExternalProvider;
using Auth.Application.Commands.UnlinkExternalProvider;
using Auth.Application.Queries.ListLinkedProviders;
using Auth.Contracts.Authorization;
using Auth.Presentation.Endpoints.ExternalProvider.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Auth.Presentation.Endpoints.ExternalProvider;

internal static class ExternalProviderEndpoints
{
    internal static void MapExternalProviderEndpoints(RouteGroupBuilder group)
    {
        // ── List: the current user's ACTIVE linked providers ─────────────────
        // B5 (2026-06-10): supersedes the earlier "no GET-list endpoint" decision —
        // the account-security page needs link state to drive the unlink UI. The
        // projection is deliberately minimal: ProviderId (the link's own id, needed
        // for unlink), provider name, provider-reported e-mail and link date.
        // ProviderUserId / tokens / raw provider payloads are never exposed.
        group.MapGet("/external-providers", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var result = await sender.Send(
                new ListLinkedProvidersQuery(currentUser.UserId.Value), ct);
            return result.ToApiResult();
        })
        .WithName("ListLinkedExternalProviders")
        .Produces<IReadOnlyList<LinkedProviderDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("List the current user's linked external OAuth providers")
        .WithMetadata(new MustHavePermissionAttribute(AuthFeatures.ExternalProvider, AppAction.Read))
        .RequireAuthorization();

        // ── Link: authenticated user binds a verified external identity ──────
        group.MapPost("/external-providers", async (
            LinkExternalProviderRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new LinkExternalProviderCommand(request.Ticket, request.RecaptchaToken), ct);
            return result.ToApiResult();
        })
        .WithName("LinkExternalProvider")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Link a verified external OAuth provider account to the current user")
        .WithMetadata(new MustHavePermissionAttribute(AuthFeatures.ExternalProvider, AppAction.Create))
        .RequireAuthorization()
        .RequireRateLimiting(RateLimitPolicies.LoginPolicy);

        // ── External login: sign in via an already-linked external provider ──
        // Uses the same rate-limit bucket as password login so abuse surface is
        // consistent across credential-bearing endpoints.
        group.MapPost("/external-providers/login", async (
            ExternalLoginRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ExternalLoginCommand(request.Ticket, request.RecaptchaToken), ct);
            var projected = result.Map(r => new ExternalLoginResponse(
                UserId: r.UserId,
                AccessToken: r.AccessToken,
                RefreshToken: r.RefreshToken,
                RefreshTokenExpiresAt: r.RefreshTokenExpiresAt));
            return projected.ToApiResult();
        })
        .WithName("ExternalLogin")
        .Produces<ExternalLoginResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Login via a previously-linked external provider using a signed BFF ticket")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.LoginPolicy);

        // ── Unlink ───────────────────────────────────────────────────────────
        group.MapDelete("/external-providers/{providerId:guid}", async (
            Guid providerId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UnlinkExternalProviderCommand(providerId), ct);
            return result.ToApiResult();
        })
        .WithName("UnlinkExternalProvider")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate a linked external OAuth provider account")
        .WithMetadata(new MustHavePermissionAttribute(AuthFeatures.ExternalProvider, AppAction.Delete))
        .RequireAuthorization();
    }
}
