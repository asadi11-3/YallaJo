using Microsoft.AspNetCore.Authorization;

namespace YallaJo.SharedKernel.Presentation.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    public const string PermissionClaimType = "Permission";

    /// <summary>
    /// Wildcard permission value. A principal holding a single claim with this
    /// value is granted EVERY permission, so super-roles (Owner / SuperAdmin)
    /// no longer need the full ~350-entry explicit Permission array baked into
    /// their JWT. Dropping that array keeps the access token small enough to fit
    /// inside the http.sys / IIS request-header size limit on shared hosting
    /// (an oversized Bearer header was being rejected with a raw HTTP 400
    /// "Request Too Long" before ASP.NET Core ever ran).
    /// </summary>
    public const string WildcardPermission = "*";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Grant if the principal holds the exact permission, OR a wildcard "*"
        // claim. The wildcard is accepted under both the canonical "Permission"
        // type and the legacy lowercase "permission" type it was historically
        // seeded under, so it works regardless of how the token was minted.
        if (context.User.HasClaim(c =>
                (c.Type == PermissionClaimType && c.Value == requirement.Permission)
                || (c.Value == WildcardPermission
                    && (c.Type == PermissionClaimType || c.Type == "permission"))))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
