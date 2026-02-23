using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Auth.Presentation;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Auth");

        // Future endpoints:
        //   POST /api/auth/login        → authenticate with credentials
        //   POST /api/auth/refresh       → refresh an access token
        //   POST /api/auth/logout        → revoke session / refresh token
        //   POST /api/auth/otp/request   → request a one-time password
        //   POST /api/auth/otp/verify    → verify a one-time password

        return endpoints;
    }
}
