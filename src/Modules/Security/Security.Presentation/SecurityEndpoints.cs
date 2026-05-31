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
        var group = endpoints.MapGroup("/api/v1/security");

        UserEndpoints.MapUserEndpoints(group.MapGroup("").WithTags("Security | Users"));
        AccountEndpoints.MapAccountEndpoints(group.MapGroup("").WithTags("Security | Accounts"));
        RoleEndpoints.MapRoleEndpoints(group.MapGroup("").WithTags("Security | Roles"));
        AuditLogEndpoints.MapAuditLogEndpoints(group.MapGroup("").WithTags("Security | Audit Logs"));

        return endpoints;
    }
}
