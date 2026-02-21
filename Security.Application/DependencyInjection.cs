using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Security.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
