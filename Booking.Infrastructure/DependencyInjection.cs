using Booking.Application.Interfaces;
using Booking.Contracts.Authorization;
using Booking.Contracts.Services;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices;
using Booking.Infrastructure.BackgroundServices.Options;
using Booking.Infrastructure.Persistence;
using Booking.Infrastructure.Persistence.Seeding;
using Booking.Infrastructure.Repositories;
using Booking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace Booking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBookingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? hostEnvironment = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<BookingDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "booking");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<BookingDbContext>, UnitOfWork<BookingDbContext>>();
        services.AddScoped<IBookingUnitOfWork, BookingUnitOfWork>();
        services.AddScoped<IModuleDbInitializer, BookingDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<BookingDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<BookingDbContext>>();
        services.TryAddSingleton(TimeProvider.System);

        // ── Repositories ─────────────────────────────────────────────────────
        services.AddScoped<ITourBookingRepository, TourBookingRepository>();
        services.AddScoped<IAvailabilitySlotRepository, AvailabilitySlotRepository>();
        services.AddScoped<IRefundPolicyRepository, RefundPolicyRepository>();
        services.AddScoped<IJoinRequestRepository, JoinRequestRepository>();
        services.AddScoped<IProviderDocumentRepository, ProviderDocumentRepository>();
        services.AddScoped<ISlotLockRepository, SlotLockRepository>();
        services.AddScoped<IGuideDiscountRepository, GuideDiscountRepository>();
        services.AddScoped<IBookingOutboxWriter, BookingOutboxWriter>();

        // ── Permission catalog ───────────────────────────────────────────────
        services.AddSingleton<IPermissionCatalog, BookingPermissionCatalog>();

        // ── Cross-module read-only services ──────────────────────────────────
        // Owned & implemented here so consumers (ContentCore, etc.) depend only on
        // Booking.Contracts and never on the Booking schema directly.
        services.AddScoped<ITourGuideOwnershipService, TourGuideOwnershipService>();
        services.AddScoped<IGuideBookingAnalyticsReader, GuideBookingAnalyticsReader>();

        // ── Booking-engine services ──────────────────────────────────────────
        services.AddScoped<IBookingReferenceGenerator, BookingReferenceGenerator>();

        // ── Cross-module snapshot readers ────────────────────────────────────
        // Real DB-backed readers are the default. Snapshots are populated via inbox
        // handlers listening to ContentTours/Accounts events. For Development we can
        // opt-in to stub readers (XOR-derived fake snapshots) so the app boots even
        // before inbox handlers have populated data:
        //   * Booking:UseStubSnapshotReaders=true → stubs (any environment, explicit).
        //   * Development environment + no flag    → stubs (convenience default).
        //   * Any other case                       → real DB-backed readers.
        // IDiscountEvaluator remains NoOp until the Promotions module is built.
        // IBookingCommissionLookup remains stub until Finance.CommissionRule is wired cross-module.
        var stubsExplicitlyEnabled = string.Equals(
            configuration["Booking:UseStubSnapshotReaders"],
            "true",
            StringComparison.OrdinalIgnoreCase);
        var stubsExplicitlyDisabled = string.Equals(
            configuration["Booking:UseStubSnapshotReaders"],
            "false",
            StringComparison.OrdinalIgnoreCase);
        var useStubs = stubsExplicitlyEnabled
            || (!stubsExplicitlyDisabled
                && hostEnvironment is not null
                && hostEnvironment.IsDevelopment());

        if (useStubs)
        {
            services.AddScoped<IBookingTourSnapshotReader, StubBookingTourSnapshotReader>();
            services.AddScoped<IBookingProviderSnapshotReader, StubBookingProviderSnapshotReader>();
            services.AddScoped<IBookingPricingSnapshotReader, StubBookingPricingSnapshotReader>();
        }
        else
        {
            services.AddScoped<IBookingTourSnapshotReader, BookingTourSnapshotReader>();
            services.AddScoped<IBookingProviderSnapshotReader, BookingProviderSnapshotReader>();
            services.AddScoped<IBookingPricingSnapshotReader, BookingPricingSnapshotReader>();
        }

        services.AddScoped<IDiscountEvaluator, NoOpDiscountEvaluator>();
        services.AddScoped<IBookingCommissionLookup, StubBookingCommissionLookup>();

        // ── Background services ──────────────────────────────────────────────
        // Singleton liveness tracker consumed by BookingBgServicesHealthCheck (registered in API host).
        services.TryAddSingleton<IBookingBackgroundServiceStatusStore, BookingBackgroundServiceStatusStore>();

        // Master-aligned services with strongly-typed Options classes.
        services.Configure<SlotLockCleanupOptions>(opts =>
        {
            var section = configuration.GetSection(SlotLockCleanupOptions.SectionName);
            ApplyBool(section, nameof(SlotLockCleanupOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(SlotLockCleanupOptions.Interval), value => opts.Interval = value);
            ApplyTimeSpan(section, nameof(SlotLockCleanupOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyInt(section, nameof(SlotLockCleanupOptions.BatchSize), value => opts.BatchSize = value);
        });

        services.Configure<BookingAutoExpireOptions>(opts =>
        {
            var section = configuration.GetSection(BookingAutoExpireOptions.SectionName);
            ApplyBool(section, nameof(BookingAutoExpireOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(BookingAutoExpireOptions.Interval), value => opts.Interval = value);
            ApplyTimeSpan(section, nameof(BookingAutoExpireOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyInt(section, nameof(BookingAutoExpireOptions.BatchSize), value => opts.BatchSize = value);
            ApplyInt(section, nameof(BookingAutoExpireOptions.PaymentWindowMinutes), value => opts.PaymentWindowMinutes = value);
        });

        services.Configure<ProviderAutoAcceptOptions>(opts =>
        {
            var section = configuration.GetSection(ProviderAutoAcceptOptions.SectionName);
            ApplyBool(section, nameof(ProviderAutoAcceptOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(ProviderAutoAcceptOptions.Interval), value => opts.Interval = value);
            ApplyTimeSpan(section, nameof(ProviderAutoAcceptOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyInt(section, nameof(ProviderAutoAcceptOptions.BatchSize), value => opts.BatchSize = value);
            ApplyDouble(section, nameof(ProviderAutoAcceptOptions.ProviderConfirmationHours), value => opts.ProviderConfirmationHours = value);
        });

        services.Configure<DocumentExpiryCheckOptions>(opts =>
        {
            var section = configuration.GetSection(DocumentExpiryCheckOptions.SectionName);
            ApplyBool(section, nameof(DocumentExpiryCheckOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeOnly(section, nameof(DocumentExpiryCheckOptions.TargetUtcTime), value => opts.TargetUtcTime = value);
            ApplyTimeSpan(section, nameof(DocumentExpiryCheckOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyInt(section, nameof(DocumentExpiryCheckOptions.BatchSize), value => opts.BatchSize = value);
            ApplyInt(section, nameof(DocumentExpiryCheckOptions.ExpiringSoonWindowDays), value => opts.ExpiringSoonWindowDays = value);
        });

        services.AddHostedService<DocumentExpiryCheckService>();   // stops first
        services.AddHostedService<ProviderAutoAcceptService>();
        services.AddHostedService<BookingAutoExpireService>();
        services.AddHostedService<SlotLockCleanupService>();       // stops last

        // ── Additional background services (workflow expansion) ──────────────
        services.Configure<JoinRequestExpiryOptions>(opts =>
        {
            var section = configuration.GetSection(JoinRequestExpiryOptions.SectionName);
            ApplyBool(section, nameof(JoinRequestExpiryOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(JoinRequestExpiryOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyTimeSpan(section, nameof(JoinRequestExpiryOptions.PollInterval), value => opts.PollInterval = value);
            ApplyInt(section, nameof(JoinRequestExpiryOptions.BatchSize), value => opts.BatchSize = value);
        });
        services.AddHostedService<JoinRequestExpiryService>();

        services.Configure<BookingAutoCompleteOptions>(opts =>
        {
            var section = configuration.GetSection(BookingAutoCompleteOptions.SectionName);
            ApplyBool(section, nameof(BookingAutoCompleteOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(BookingAutoCompleteOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyTimeSpan(section, nameof(BookingAutoCompleteOptions.PollInterval), value => opts.PollInterval = value);
            ApplyInt(section, nameof(BookingAutoCompleteOptions.BatchSize), value => opts.BatchSize = value);
        });
        services.AddHostedService<BookingAutoCompleteService>();

        services.Configure<SlotGenerationOptions>(opts =>
        {
            var section = configuration.GetSection(SlotGenerationOptions.SectionName);
            ApplyBool(section, nameof(SlotGenerationOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(SlotGenerationOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyTimeSpan(section, nameof(SlotGenerationOptions.PollInterval), value => opts.PollInterval = value);
            ApplyInt(section, nameof(SlotGenerationOptions.WindowDays), value => opts.WindowDays = value);
            ApplyInt(section, nameof(SlotGenerationOptions.DefaultMaxCapacity), value => opts.DefaultMaxCapacity = value);
        });
        services.AddHostedService<SlotGenerationService>();

        services.Configure<BookingReminderOptions>(opts =>
        {
            var section = configuration.GetSection(BookingReminderOptions.SectionName);
            ApplyBool(section, nameof(BookingReminderOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(BookingReminderOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyTimeSpan(section, nameof(BookingReminderOptions.PollInterval), value => opts.PollInterval = value);
            ApplyInt(section, nameof(BookingReminderOptions.ReminderHoursBeforeStart), value => opts.ReminderHoursBeforeStart = value);
            ApplyInt(section, nameof(BookingReminderOptions.BatchSize), value => opts.BatchSize = value);
        });
        services.AddHostedService<BookingReminderService>();

        services.Configure<SlotCleanupOptions>(opts =>
        {
            var section = configuration.GetSection(SlotCleanupOptions.SectionName);
            ApplyBool(section, nameof(SlotCleanupOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(SlotCleanupOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyTimeSpan(section, nameof(SlotCleanupOptions.PollInterval), value => opts.PollInterval = value);
            ApplyInt(section, nameof(SlotCleanupOptions.RetentionDays), value => opts.RetentionDays = value);
            ApplyInt(section, nameof(SlotCleanupOptions.BatchSize), value => opts.BatchSize = value);
        });
        services.AddHostedService<SlotCleanupService>();

        return services;
    }

    private static void ApplyBool(IConfiguration section, string key, Action<bool> apply)
    {
        if (bool.TryParse(section[key], out var value))
        {
            apply(value);
        }
    }

    private static void ApplyInt(IConfiguration section, string key, Action<int> apply)
    {
        if (int.TryParse(section[key], out var value) && value > 0)
        {
            apply(value);
        }
    }

    private static void ApplyDouble(IConfiguration section, string key, Action<double> apply)
    {
        if (double.TryParse(
                section[key],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value)
            && value > 0d)
        {
            apply(value);
        }
    }

    private static void ApplyTimeSpan(IConfiguration section, string key, Action<TimeSpan> apply)
    {
        if (TimeSpan.TryParse(section[key], out var value) && value > TimeSpan.Zero)
        {
            apply(value);
        }
    }

    private static void ApplyTimeOnly(IConfiguration section, string key, Action<TimeOnly> apply)
    {
        if (TimeOnly.TryParse(section[key], System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            apply(value);
        }
    }
}
