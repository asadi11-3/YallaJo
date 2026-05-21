using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Contracts.Authorization;
using Messaging.Contracts.Services;
using Messaging.Domain.Repositories;
using Messaging.Infrastructure.BackgroundServices;
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

        // ── Repositories ──
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationDeliveryAttemptRepository, NotificationDeliveryAttemptRepository>();
        services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<IUserSnapshotRepository, UserSnapshotRepository>();
        services.AddScoped<IAdminAssignmentRosterRepository, AdminAssignmentRosterRepository>();
        services.AddScoped<IMessagingOutboxWriter, MessagingOutboxWriter>();

        // ── Notification delivery abstractions (real implementations) ──
        services.AddScoped<INotificationTemplateRenderer, MustacheNotificationTemplateRenderer>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<INotificationChannelStrategy, InAppNotificationStrategy>();
        services.AddScoped<INotificationChannelStrategy, EmailNotificationStrategy>();
        services.AddScoped<INotificationChannelStrategy, PushNotificationStrategy>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        var emailSenderOptions = BindEmailSenderOptions(configuration);
        var cleanupOptions = BindReadNotificationCleanupOptions(configuration);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(emailSenderOptions));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(cleanupOptions));
        services.AddHostedService<EmailNotificationSenderService>();
        services.AddHostedService<ReadNotificationCleanupService>();

        // ── Permission catalog ──
        services.AddSingleton<IPermissionCatalog, MessagingPermissionCatalog>();

        return services;
    }

    private static EmailNotificationSenderOptions BindEmailSenderOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(EmailNotificationSenderOptions.SectionName);
        return new EmailNotificationSenderOptions
        {
            Enabled = GetBool(section, nameof(EmailNotificationSenderOptions.Enabled), true),
            Interval = GetTimeSpan(section, nameof(EmailNotificationSenderOptions.Interval), TimeSpan.FromSeconds(30)),
            InitialDelay = GetTimeSpan(section, nameof(EmailNotificationSenderOptions.InitialDelay), TimeSpan.FromMinutes(2)),
            MaxRetries = GetInt(section, nameof(EmailNotificationSenderOptions.MaxRetries), 3),
            BatchSize = GetInt(section, nameof(EmailNotificationSenderOptions.BatchSize), 50),
        };
    }

    private static ReadNotificationCleanupOptions BindReadNotificationCleanupOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(ReadNotificationCleanupOptions.SectionName);
        return new ReadNotificationCleanupOptions
        {
            Enabled = GetBool(section, nameof(ReadNotificationCleanupOptions.Enabled), true),
            TargetDayOfWeek = GetEnum(section, nameof(ReadNotificationCleanupOptions.TargetDayOfWeek), DayOfWeek.Sunday),
            TargetTimeUtc = GetTimeOnly(section, nameof(ReadNotificationCleanupOptions.TargetTimeUtc), new TimeOnly(2, 0)),
            RetentionDays = GetInt(section, nameof(ReadNotificationCleanupOptions.RetentionDays), 30),
            MaxPerUser = GetInt(section, nameof(ReadNotificationCleanupOptions.MaxPerUser), 500),
        };
    }

    private static bool GetBool(IConfiguration section, string key, bool fallback)
        => bool.TryParse(section[key], out var value) ? value : fallback;

    private static int GetInt(IConfiguration section, string key, int fallback)
        => int.TryParse(section[key], out var value) ? value : fallback;

    private static TimeSpan GetTimeSpan(IConfiguration section, string key, TimeSpan fallback)
        => TimeSpan.TryParse(section[key], out var value) ? value : fallback;

    private static TimeOnly GetTimeOnly(IConfiguration section, string key, TimeOnly fallback)
        => TimeOnly.TryParse(section[key], out var value) ? value : fallback;

    private static TEnum GetEnum<TEnum>(IConfiguration section, string key, TEnum fallback)
        where TEnum : struct
        => Enum.TryParse<TEnum>(section[key], ignoreCase: true, out var value) ? value : fallback;
}
