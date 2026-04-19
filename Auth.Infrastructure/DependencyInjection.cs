using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Persistence.Seeding;
using Auth.Infrastructure.Repositories;
using Auth.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using Auth.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;

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
        services.AddScoped<IModuleDbInitializer, AuthDbInitializer>();
        services.AddScoped<IAuthUnitOfWork, AuthUnitOfWork>();

        // Repositories
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IExternalProviderRepository, ExternalProviderRepository>();

        // Inbox — consumer-side idempotency store for integration event handlers
        services.AddScoped<IAuthInboxStore, AuthInboxStore>();

        // Application services
        services.AddSingleton<IOtpService, OtpService>();
        services.AddSingleton<IInviteTokenService, InviteTokenService>();
        services.Configure<InviteOptions>(configuration.GetSection(InviteOptions.SectionName));
        services.AddSingleton<IInviteLinkBuilder, InviteLinkBuilder>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.Configure<GmailOptions>(configuration.GetSection(GmailOptions.SectionName));
        services.AddScoped<IEmailService, GmailEmailService>();

        // MediatR handlers in this assembly (integration event handlers)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddScoped<IOutboxProcessor, OutboxProcessor<AuthDbContext>>();
        services.AddHostedService<AuthCleanupService>();

        // Distributed cache — idempotent, safe if the host already registered it
        services.AddHybridCache();

        return services;
    }
}
