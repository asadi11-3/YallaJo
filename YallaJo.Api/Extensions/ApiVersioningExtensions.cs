using Asp.Versioning;

namespace YallaJo.Api.Extensions;

/// <summary>
/// Configures API versioning using URL segment strategy (/api/v1/...).
/// All existing endpoints default to v1. Future breaking changes get v2+.
/// </summary>
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddYallaJoApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;

            // URL segment: /api/v1/content-core/categories
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        return services;
    }
}
