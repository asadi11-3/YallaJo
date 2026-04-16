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
        group.MapPost("/external-providers", async (LinkExternalProviderRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new LinkExternalProviderCommand(
                request.Provider,
                request.ProviderUserId,
                request.ProviderEmail), ct);
            return result.ToApiResult();
        })
        .WithName("LinkExternalProvider")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Link an external OAuth provider account to the current user")
        .RequireAuthorization();

        group.MapDelete("/external-providers/{providerId:guid}", async (Guid providerId, ISender sender, CancellationToken ct) =>
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
