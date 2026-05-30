using ContentBlogs.Presentation.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ContentBlogs.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddContentBlogsPresentation(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AnonymousViewerProvider>();

        return services;
    }
}
