using Auth.Application.Recaptcha;
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

            // reCAPTCHA v3 pipeline guard — MUST run before handler execution
            // for every command that implements IRecaptchaProtectedCommand.
            // Keeping it here (open generic) means no handler can forget the
            // check and no handler can accidentally bypass it.
            cfg.AddOpenBehavior(typeof(RecaptchaValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
