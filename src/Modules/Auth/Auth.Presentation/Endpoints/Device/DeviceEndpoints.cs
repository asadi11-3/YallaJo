using Auth.Contracts.Authorization;
using Auth.Application.Commands.TrustDevice;
using Auth.Application.Commands.UntrustDevice;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Auth.Presentation.Endpoints.Device;

internal static class DeviceEndpoints
{
    internal static void MapDeviceEndpoints(RouteGroupBuilder group)
    {
        group.MapPatch("/devices/{deviceId:guid}/trust", async (Guid deviceId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new TrustDeviceCommand(deviceId), ct);
            return result.ToApiResult();
        })
        .WithName("TrustDevice")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Mark a device as trusted for the current user")
        .WithMetadata(new MustHavePermissionAttribute(AuthFeatures.Device, AppAction.Update))
        .RequireAuthorization();

        group.MapDelete("/devices/{deviceId:guid}/trust", async (Guid deviceId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UntrustDeviceCommand(deviceId), ct);
            return result.ToApiResult();
        })
        .WithName("UntrustDevice")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove the trusted mark from a device for the current user")
        .WithMetadata(new MustHavePermissionAttribute(AuthFeatures.Device, AppAction.Update))
        .RequireAuthorization();
    }
}
