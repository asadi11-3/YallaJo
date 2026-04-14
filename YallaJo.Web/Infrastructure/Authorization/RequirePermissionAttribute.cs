using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YallaJo.Web.Infrastructure.Identity;

namespace YallaJo.Web.Infrastructure.Authorization;

/// <summary>
/// Declarative page-level or action-level authorization attribute.
///
/// Usage:
///   [RequirePermission(WebPermission.Role.Read)]         // on controller class
///   [RequirePermission(WebPermission.Role.Delete)]       // on a stricter action
///
/// Behaviour:
///   - Unauthenticated request  → ChallengeResult  (cookie middleware redirects to login)
///   - Authenticated, no perm   → ForbidResult     (ForbiddenResultFilter converts to 403 response)
///   - Authenticated, has perm  → passes through normally
///
/// Multiple attributes on the same target are AND-ed (all permissions required).
/// The check is performed via ICurrentUser, which is the only permitted path for
/// permission queries in the entire Web project.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    /// <summary>The permission constant this attribute enforces (e.g. WebPermission.Role.Read).</summary>
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
        : base(typeof(RequirePermissionFilter))
    {
        Permission = permission;
        Arguments  = [permission];
        Order      = int.MinValue; // run before other filters
    }
}

/// <summary>Inner filter — created by the DI container so it can receive ICurrentUser.</summary>
internal sealed class RequirePermissionFilter : IAsyncAuthorizationFilter
{
    private readonly ICurrentUser _currentUser;
    private readonly string       _permission;

    public RequirePermissionFilter(ICurrentUser currentUser, string permission)
    {
        _currentUser = currentUser;
        _permission  = permission;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!_currentUser.IsAuthenticated)
        {
            context.Result = new ChallengeResult();
            return Task.CompletedTask;
        }

        if (!_currentUser.HasPermission(_permission))
            context.Result = new ForbidResult();

        return Task.CompletedTask;
    }
}
