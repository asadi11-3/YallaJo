using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Auth.Presentation;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");
        // TODO: Add auth endpoints (login, refresh token, logout)
        return endpoints;
    }
}
