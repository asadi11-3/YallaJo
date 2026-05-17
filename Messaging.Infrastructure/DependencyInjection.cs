using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Contracts.Authorization;
using Messaging.Contracts.Services;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using Messaging.Infrastructure.Persistence.Seeding;
using Messaging.Infrastructure.Repositories;
using Messaging.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Messaging.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMessagingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<MessagingDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "messaging");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<MessagingDbContext>, UnitOfWork<MessagingDbContext>>();
        services.AddScoped<IMessagingUnitOfWork, MessagingUnitOfWork>();
        services.AddScoped<IMessagingInboxStore, MessagingInboxStore>();
        services.AddScoped<IModuleDbInitializer, MessagingDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<MessagingDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<MessagingDbContext>>();

        // Repositories (6)
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<IMessagingOutboxWriter, MessagingOutboxWriter>();

        // Notification delivery abstractions (Noop stubs — sprint team replaces)
        services.AddScoped<INotificationDispatcher, NoopNotificationDispatcher>();
        services.AddScoped<INotificationTemplateRenderer, NoopNotificationTemplateRenderer>();
        services.AddScoped<IEmailSender, NoopEmailSender>();

        // Permission catalog
        services.AddSingleton<IPermissionCatalog, MessagingPermissionCatalog>();

        return services;
    }
}
