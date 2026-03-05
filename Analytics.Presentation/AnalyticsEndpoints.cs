using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Analytics.Presentation;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
