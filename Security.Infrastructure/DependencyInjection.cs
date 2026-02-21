using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Security.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Register SecurityDbContext, repositories
        return services;
    }
}
