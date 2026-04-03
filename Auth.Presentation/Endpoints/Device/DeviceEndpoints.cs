using Auth.Application.Commands.TrustDevice;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Presentation;

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
        .RequireAuthorization();
    }
}
