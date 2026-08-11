using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tracking.Presentation.Endpoints.Trakcing;

namespace Tracking.Presentation;

public static class TrackingEndpoints
{
    public static IEndpointRouteBuilder MapTrackingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var trackingGroup = endpoints.MapGroup("/api/v1/tracking");

        TrackingSessionEndpoints.MapTrackingSessionEndpoints(
            trackingGroup.MapGroup("").WithTags("Tracking | Sessions"));

        return endpoints;
    }
}
