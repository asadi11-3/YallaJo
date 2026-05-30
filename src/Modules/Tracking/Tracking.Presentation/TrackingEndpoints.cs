using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Tracking.Presentation;

public static class TrackingEndpoints
{
    public static IEndpointRouteBuilder MapTrackingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
