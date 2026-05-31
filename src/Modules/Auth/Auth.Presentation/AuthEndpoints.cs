using Auth.Presentation.Endpoints.Credential;
using Auth.Presentation.Endpoints.Device;
using Auth.Presentation.Endpoints.ExternalProvider;
using Auth.Presentation.Endpoints.Invitation;
using Auth.Presentation.Endpoints.Registration;
using Auth.Presentation.Endpoints.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Auth.Presentation;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth");

        RegistrationEndpoints.MapRegistrationEndpoints(group.MapGroup("").WithTags("Auth | Registration"));
        InvitationEndpoints.MapInvitationEndpoints(group.MapGroup("").WithTags("Auth | Invitations"));
        CredentialEndpoints.MapCredentialEndpoints(group.MapGroup("").WithTags("Auth | Credentials"));
        SessionEndpoints.MapSessionEndpoints(group.MapGroup("").WithTags("Auth | Sessions"));
        DeviceEndpoints.MapDeviceEndpoints(group.MapGroup("").WithTags("Auth | Devices"));
        ExternalProviderEndpoints.MapExternalProviderEndpoints(group.MapGroup("").WithTags("Auth | External Providers"));

        return endpoints;
    }
}
