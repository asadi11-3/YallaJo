using Auth.Presentation.Endpoints.Credential;
using Auth.Presentation.Endpoints.Device;
using Auth.Presentation.Endpoints.ExternalProvider;
using Auth.Presentation.Endpoints.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Auth.Presentation;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        CredentialEndpoints.MapCredentialEndpoints(group);
        SessionEndpoints.MapSessionEndpoints(group);
        DeviceEndpoints.MapDeviceEndpoints(group);
        ExternalProviderEndpoints.MapExternalProviderEndpoints(group);

        return endpoints;
    }
}
