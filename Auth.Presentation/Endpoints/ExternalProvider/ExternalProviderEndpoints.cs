using Auth.Application.Commands.ExternalLogin;
using Auth.Application.Commands.LinkExternalProvider;
using Auth.Application.Commands.UnlinkExternalProvider;
using Auth.Presentation.Endpoints.ExternalProvider.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Presentation;

namespace Auth.Presentation.Endpoints.ExternalProvider;

internal static class ExternalProviderEndpoints
{
    internal static void MapExternalProviderEndpoints(RouteGroupBuilder group)
    {
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
        .RequireAuthorization();
    }
}
