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

        // Bulk session/refresh-token revocation used by credential and admin
        // lifecycle flows (self-service reset, activation, admin reset,
        // suspend, archive).
        services.AddScoped<ISessionRevocationService, SessionRevocationService>();

        return services;
    }
}
