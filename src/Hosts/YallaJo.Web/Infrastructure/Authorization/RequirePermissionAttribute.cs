using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YallaJo.Web.Infrastructure.Identity;

namespace YallaJo.Web.Infrastructure.Authorization;

 [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(typeof(RequirePermissionFilter))
    {
        Permission = permission;
        Arguments  = [permission];
        Order      = int.MinValue;
    }

    public string Permission { get; }
}

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
