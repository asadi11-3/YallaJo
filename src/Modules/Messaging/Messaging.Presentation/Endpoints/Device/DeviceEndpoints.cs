using MediatR;
using Messaging.Application.Commands.DeleteDeviceToken;
using Messaging.Application.Commands.RegisterDeviceToken;
using Messaging.Application.Queries.GetMyDeviceTokens;
using Messaging.Contracts.Authorization;
using Messaging.Presentation.Endpoints.Device.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Messaging.Presentation.Endpoints.Device;

internal static class DeviceEndpoints
{
    internal static void MapDeviceEndpoints(RouteGroupBuilder group)
    {
        group.WithTags("Messaging | Devices");

        group.MapPost("/token", async (
            RegisterDeviceTokenRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RegisterDeviceTokenCommand(
                currentUser.UserId!.Value,
                request.DeviceId,
                request.Platform,
                request.Token);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("RegisterDeviceToken")
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Register a device token for push notifications.")
        .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.DeviceToken, AppAction.Create))
        .RequireAuthorization();

        group.MapDelete("/token/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteDeviceTokenCommand(id, currentUser.UserId!.Value), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToApiResult();
        })
        .WithName("DeleteDeviceToken")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete a registered device token.")
        .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.DeviceToken, AppAction.Delete))
        .RequireAuthorization();

        group.MapGet("/tokens", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyDeviceTokensQuery(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyDeviceTokens")
        .Produces<IReadOnlyList<DeviceTokenDto>>(StatusCodes.Status200OK)
        .WithSummary("Get the current user's registered device tokens.")
        .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.DeviceToken, AppAction.Read))
        .RequireAuthorization();
    }
}
