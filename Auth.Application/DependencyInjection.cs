using Auth.Application.Interfaces;
using Auth.Application.Recaptcha;
using Auth.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(RecaptchaValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Phase 1 — bulk session/refresh-token revocation used by credential
        // mutation flows (self-service reset, activation). Phase 2 will add
        // admin reset / reassignment / suspend / archive call sites.
        services.AddScoped<ISessionRevocationService, SessionRevocationService>();

        return services;
    }
}
