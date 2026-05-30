using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Tracking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTrackingApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
