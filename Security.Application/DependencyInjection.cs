using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Security.Application.Authorization;
using Security.Application.Services;
using Security.Contracts.Abstractions;

namespace Security.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IRoleHierarchyService, RoleHierarchyService>();

        return services;
    }
}
