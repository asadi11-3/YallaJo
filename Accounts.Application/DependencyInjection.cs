using Microsoft.Extensions.DependencyInjection;

namespace Accounts.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsApplication(
        this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}