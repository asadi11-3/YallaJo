using Auth.Application.Interfaces;
using Auth.Application.Recaptcha;
using Auth.Domain.Repositories;
using Auth.Infrastructure.ExternalAuth;
using Auth.Infrastructure.Outbox;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Persistence.Seeding;
using Auth.Infrastructure.Recaptcha;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Infrastructure.Outbox;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using Auth.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;
using Auth.Application.Interfaces.ExternalAuth;
using Auth.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "auth");
                    sql.EnableRetryOnFailure(3);
                }));

        // UnitOfWork wraps AuthDbContext and dispatches domain events on SaveChanges
        services.AddScoped<IUnitOfWork<AuthDbContext>, UnitOfWork<AuthDbContext>>();
        services.AddSingleton<IPermissionCatalog, AuthPermissionCatalog>();
        services.AddScoped<IModuleDbInitializer, AuthDbInitializer>();
        services.AddScoped<IAuthUnitOfWork, AuthUnitOfWork>();

        // Retriable cross-module transaction executor used by VerifyEmail +
        // ResetPassword to enlist Security writes with Auth writes under a
        // single EF execution strategy. See AuthTransactionalExecutor for the
        // rationale (EnableRetryOnFailure rejects user-initiated transactions
        // unless run via CreateExecutionStrategy()).
        services.AddScoped<ITransactionalExecutor, AuthTransactionalExecutor>();

        // Repositories
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IActivationTokenRepository, ActivationTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IExternalProviderRepository, ExternalProviderRepository>();

        // Inbox — consumer-side idempotency store for integration event handlers
        services.AddScoped<IAuthInboxStore, AuthInboxStore>();

        // Outbox — producer-side writer that enqueues outbound integration events
        services.AddScoped<IAuthOutboxWriter, AuthOutboxWriter>();

        // Application services
        services.AddSingleton<IOtpService, OtpService>();
        services.AddSingleton<IInviteTokenService, InviteTokenService>();
        services.Configure<InviteOptions>(configuration.GetSection(InviteOptions.SectionName));
        services.AddSingleton<IInviteLinkBuilder, InviteLinkBuilder>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<ITokenService, JwtTokenService>();

        // External-provider auth ticket protocol (Google / Facebook / ...).
        // Options are validated at startup so a missing signing key fails fast
        // instead of silently accepting forged tickets.
        services.AddOptions<ExternalAuthOptions>()
            .Bind(configuration.GetSection(ExternalAuthOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ExternalAuthOptions>, ExternalAuthOptionsValidator>();
        services.AddSingleton<IExternalAuthTicketVerifier, ExternalAuthTicketVerifier>();
        services.AddSingleton<IExternalAuthNonceStore, HybridCacheExternalAuthNonceStore>();

         // reCAPTCHA v3 bot protection. Verifier is invoked automatically via
         // the RecaptchaValidationBehavior pipeline step on any command that
         // implements IRecaptchaProtectedCommand — there is no way to bypass
         // the check from an individual handler.
         // RECAPTCHA DISABLED - TODO: uncomment when re-enabling
         /*
         services.AddOptions<RecaptchaOptions>()
             .Bind(configuration.GetSection(RecaptchaOptions.SectionName))
             .ValidateOnStart();
         services.AddSingleton<IValidateOptions<RecaptchaOptions>, RecaptchaOptionsValidator>();
         services.AddHttpClient(GoogleRecaptchaVerifier.HttpClientName, (sp, client) =>
         {
             var opts = sp.GetRequiredService<IOptions<RecaptchaOptions>>().Value;
             client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
         });
         services.AddSingleton<IRecaptchaVerifier, GoogleRecaptchaVerifier>();
         */
        services.Configure<GmailOptions>(configuration.GetSection(GmailOptions.SectionName));
        services.AddScoped<IEmailService, GmailEmailService>();

        // MediatR handlers in this assembly (integration event handlers)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddScoped<IOutboxProcessor, OutboxProcessor<AuthDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<AuthDbContext>>();

        // Retention policy for disposable Auth state.
        // AuthCleanupService (BackgroundService) runs the loop; the
        // worker does the actual work and is unit-testable in isolation.
        // SC-2: the hosted service is always registered, but its loop
        // is gated at runtime on Auth:Retention:Enabled so cleanup runs
        // from a single host only (API host enables; Web/test hosts
        // disable) without needing a redeploy to toggle.
        services.Configure<AuthRetentionOptions>(
            configuration.GetSection(AuthRetentionOptions.SectionName));
        services.AddScoped<IRetentionDeleteAdapter, EfExecuteDeleteAdapter>();
        services.AddScoped<IAuthRetentionWorker, AuthRetentionWorker>();
        services.AddHostedService<AuthCleanupService>();

        // Distributed cache — idempotent, safe if the host already registered it
        services.AddHybridCache();

        return services;
    }
}
