using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMessagingApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
