using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Presentation.Endpoints.Account;
using Security.Presentation.Endpoints.AuditLog;
using Security.Presentation.Endpoints.Role;
using Security.Presentation.Endpoints.User;

namespace Security.Presentation;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/security")
            .WithTags("Security");

        UserEndpoints.MapUserEndpoints(group);
        AccountEndpoints.MapAccountEndpoints(group);
        RoleEndpoints.MapRoleEndpoints(group);
        AuditLogEndpoints.MapAuditLogEndpoints(group);

        return endpoints;
    }
}
