using System.Reflection;
using YallaJo.Web.Services;

namespace YallaJo.Web.Infrastructure.DependencyInjection;

/// <summary>
/// Convention-based registration for feature-folder services.
/// Every concrete, non-abstract class whose name ends in <c>ApiClient</c> or
/// <c>Facade</c> is registered as a scoped self-binding, replacing the ~40
/// hand-written <c>AddScoped&lt;XxxApiClient&gt;()</c> / <c>AddScoped&lt;XxxFacade&gt;()</c>
/// pairs that previously lived in Program.cs.
/// </summary>
public static class FeatureServiceRegistration
{
    /// <summary>
    /// Scans the web assembly and registers all feature ApiClients and Facades as scoped.
    /// </summary>
    /// <remarks>
    /// The base <see cref="ApiClient"/> typed client is intentionally excluded: it is
    /// registered via <c>AddHttpClient&lt;ApiClient&gt;()</c> (and exposed as
    /// <see cref="IApiClient"/>) so it keeps its resilience pipeline and JWT handler.
    /// </remarks>
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        var assembly = typeof(FeatureServiceRegistration).Assembly;

        var featureTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t != typeof(ApiClient))
            .Where(t => t.Name.EndsWith("ApiClient", StringComparison.Ordinal)
                        || t.Name.EndsWith("Facade", StringComparison.Ordinal));

        foreach (var type in featureTypes)
        {
            services.AddScoped(type);
        }

        return services;
    }
}
