using Booking.Application.Interfaces;
using Booking.Contracts.Authorization;
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
        services.AddScoped<ICommissionSnapshotRepository, CommissionSnapshotRepository>();
        services.AddScoped<IJoinRequestRepository, JoinRequestRepository>();
        services.AddScoped<IProviderDocumentRepository, ProviderDocumentRepository>();
        services.AddScoped<ISlotLockRepository, SlotLockRepository>();
        services.AddScoped<IBookingOutboxWriter, BookingOutboxWriter>();
        services.AddScoped<IBookingInboxStore, BookingInboxStore>();

        // ── Permission catalog ───────────────────────────────────────────────
        services.AddSingleton<IPermissionCatalog, BookingPermissionCatalog>();

        // ── Cross-module read-only services ──────────────────────────────────
        // Owned & implemented here so consumers (ContentCore, etc.) depend only on
        // Booking.Contracts and never on the Booking schema directly.
        services.AddScoped<ITourGuideOwnershipService, TourGuideOwnershipService>();

        // ── Booking-engine services ──────────────────────────────────────────
        services.AddScoped<IBookingReferenceGenerator, BookingReferenceGenerator>();

        // ── Cross-module snapshot readers ────────────────────────────────────
        // TODO: Swap these stubs for the real snapshot readers when:
        //   * ContentTours ships the booking.TourSnapshots inbox handler
        //     (replace IBookingTourSnapshotReader + IBookingPricingSnapshotReader)
        //   * Identity ships the booking.ProviderSnapshots inbox handler
        //     (replace IBookingProviderSnapshotReader)
        //   * Promotions module is built
        //     (replace IDiscountEvaluator)
        //
        // The stubs return XOR-derived FAKE snapshots for any non-empty GUID, which is
        // acceptable for Development but UNSAFE for Production. We pick the wiring based on
        // the hosting environment + an explicit opt-in flag:
        //   * Development                             → stubs.
        //   * Non-Development + AllowStubSnapshotReaders=true → stubs.
        //   * Anything else (incl. null env)          → throwing impls that fail fast at
        //                                                first call with a clear error.
        var allowStubsFlag = string.Equals(
            configuration["Booking:AllowStubSnapshotReaders"],
            "true",
            StringComparison.OrdinalIgnoreCase);
        var useStubs = allowStubsFlag
            || (hostEnvironment is not null && hostEnvironment.IsDevelopment());

        if (useStubs)
        {
            services.AddScoped<IBookingTourSnapshotReader, StubBookingTourSnapshotReader>();
            services.AddScoped<IBookingProviderSnapshotReader, StubBookingProviderSnapshotReader>();
            services.AddScoped<IBookingPricingSnapshotReader, StubBookingPricingSnapshotReader>();
        }
        else
        {
            services.AddScoped<IBookingTourSnapshotReader, ThrowingBookingTourSnapshotReader>();
            services.AddScoped<IBookingProviderSnapshotReader, ThrowingBookingProviderSnapshotReader>();
            services.AddScoped<IBookingPricingSnapshotReader, ThrowingBookingPricingSnapshotReader>();
        }

        services.AddScoped<IDiscountEvaluator, NoOpDiscountEvaluator>();

        services.Configure<BookingCommissionDefaultsOptions>(opts =>
        {
            var section = configuration.GetSection(BookingCommissionDefaultsOptions.SectionName);
            var tier = section[nameof(BookingCommissionDefaultsOptions.Tier)];
            if (!string.IsNullOrWhiteSpace(tier))
            {
                opts.Tier = tier;
            }
            var currency = section[nameof(BookingCommissionDefaultsOptions.Currency)];
            if (!string.IsNullOrWhiteSpace(currency))
            {
                opts.Currency = currency;
            }
            ApplyDecimal(section, nameof(BookingCommissionDefaultsOptions.FallbackRate), value => opts.FallbackRate = value);
        });
        services.AddScoped<IBookingCommissionLookup, SnapshotBookingCommissionLookup>();

        // ── Background services (TASK 7) ─────────────────────────────────────
        // Singleton liveness tracker consumed by BookingBgServicesHealthCheck (registered in API host).
        services.TryAddSingleton<IBookingBackgroundServiceStatusStore, BookingBackgroundServiceStatusStore>();

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

    private static void ApplyDecimal(IConfiguration section, string key, Action<decimal> apply)
    {
        if (decimal.TryParse(
                section[key],
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value)
            && value >= 0m)
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
