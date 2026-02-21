using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Security.Presentation;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/security");
        // TODO: Add security endpoints (roles, permissions)
        return endpoints;
    }
}
