using Accounts.Application.Services;
using Accounts.Contracts.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Accounts.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsApplication(
        this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Contract-based capability exposed to other modules (Auth invite
        // orchestration) to create a profile for a newly invited user without
        // violating the module boundary.
        services.AddScoped<IProfileCreationService, ProfileCreationService>();

        // Phase 3D — cross-module capability used by the Auth admin
        // reassignment flow to scrub the target user's profile inside
        // the same transactional scope as the Security/Auth
        // reassignment.
        services.AddScoped<IProfileReassignmentService, ProfileReassignmentService>();

        return services;
    }
}
