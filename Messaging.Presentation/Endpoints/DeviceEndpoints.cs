using MediatR;
using Messaging.Application.Commands.DeleteDeviceToken;
using Messaging.Application.Commands.RegisterDeviceToken;
using Messaging.Application.Queries.GetMyDeviceTokens;
using Messaging.Contracts.Authorization;
using Messaging.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Messaging.Presentation.Endpoints;

internal static class DeviceEndpoints
{
    internal static RouteGroupBuilder MapDeviceEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/token", async (
            RegisterDeviceTokenRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Result.Failure<Guid>(new Error("DeviceToken.Unauthorized", "Not authenticated"), Outcome.Unauthorized).ToApiResult();
            var cmd = new RegisterDeviceTokenCommand(
                currentUser.UserId.Value,
                request.DeviceId,
                request.Platform,
                request.Token);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        }).WithName("RegisterDeviceToken").WithTags("Devices")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.DeviceToken, AppAction.Create))
          .RequireAuthorization();

        group.MapDelete("/token/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();
            var result = await sender.Send(new DeleteDeviceTokenCommand(id, currentUser.UserId.Value), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToApiResult();
        }).WithName("DeleteDeviceToken").WithTags("Devices")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.DeviceToken, AppAction.Delete))
          .RequireAuthorization();

        group.MapGet("/tokens", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Result.Failure<object>(new Error("DeviceToken.Unauthorized", "Not authenticated"), Outcome.Unauthorized).ToApiResult();
            var result = await sender.Send(new GetMyDeviceTokensQuery(currentUser.UserId.Value), ct);
            return result.ToApiResult();
        }).WithName("GetMyDeviceTokens").WithTags("Devices")
          .WithMetadata(new MustHavePermissionAttribute(MessagingFeatures.DeviceToken, AppAction.Read))
          .RequireAuthorization();

        return group;
    }
}

internal sealed record RegisterDeviceTokenRequest(string DeviceId, DevicePlatform Platform, string Token);
