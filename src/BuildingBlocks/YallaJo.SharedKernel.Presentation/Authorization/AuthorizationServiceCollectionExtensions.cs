using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace YallaJo.SharedKernel.Presentation.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Wires the permission-based authorization pipeline.
    /// <list type="bullet">
    ///   <item><see cref="PermissionPolicyProvider"/> — dynamically builds policies for any "Permission.{Feature}.{Action}" name.</item>
    ///   <item><see cref="PermissionAuthorizationHandler"/> — checks the user's "Permission" claims for a match.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
