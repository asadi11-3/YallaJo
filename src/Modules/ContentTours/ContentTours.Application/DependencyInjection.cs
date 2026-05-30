using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ContentTours.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddContentToursApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
