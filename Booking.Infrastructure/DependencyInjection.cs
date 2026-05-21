using Booking.Application.Interfaces;
using Booking.Contracts.Authorization;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices;
using Booking.Infrastructure.Persistence;
using Booking.Infrastructure.Persistence.Seeding;
using Booking.Infrastructure.Repositories;
using Booking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Outbox;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace Booking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBookingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
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
        services.AddScoped<IBookingOutboxWriter, BookingOutboxWriter>();

        // ── Permission catalog ───────────────────────────────────────────────
        services.AddSingleton<IPermissionCatalog, BookingPermissionCatalog>();

        // ── Cross-module read-only services ──────────────────────────────────
        // Owned & implemented here so consumers (ContentCore, etc.) depend only on
        // Booking.Contracts and never on the Booking schema directly.
        services.AddScoped<ITourGuideOwnershipService, TourGuideOwnershipService>();

        // ── Booking-engine services ──────────────────────────────────────────
        services.AddScoped<IBookingReferenceGenerator, BookingReferenceGenerator>();

        // ── Cross-module snapshot readers (STUB IMPLS) ───────────────────────
        // TODO: Swap these stubs for the real snapshot readers when:
        //   * ContentTours ships the booking.TourSnapshots inbox handler
        //     (replace IBookingTourSnapshotReader + IBookingPricingSnapshotReader)
        //   * Identity ships the booking.ProviderSnapshots inbox handler
        //     (replace IBookingProviderSnapshotReader)
        //   * Promotions module is built
        //     (replace IDiscountEvaluator)
        services.AddScoped<IBookingTourSnapshotReader, StubBookingTourSnapshotReader>();
        services.AddScoped<IBookingProviderSnapshotReader, StubBookingProviderSnapshotReader>();
        services.AddScoped<IBookingPricingSnapshotReader, StubBookingPricingSnapshotReader>();
        services.AddScoped<IDiscountEvaluator, NoOpDiscountEvaluator>();
        services.AddScoped<IBookingCommissionLookup, StubBookingCommissionLookup>();

        // ── Background services ──────────────────────────────────────────────
        services.Configure<SlotLockCleanupOptions>(opts =>
        {
            var section = configuration.GetSection(SlotLockCleanupOptions.SectionName);
            ApplyBool(section, nameof(SlotLockCleanupOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(SlotLockCleanupOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyTimeSpan(section, nameof(SlotLockCleanupOptions.PollInterval), value => opts.PollInterval = value);
            ApplyInt(section, nameof(SlotLockCleanupOptions.BatchSize), value => opts.BatchSize = value);
        });
        services.Configure<DocumentExpiryCheckOptions>(opts =>
        {
            var section = configuration.GetSection(DocumentExpiryCheckOptions.SectionName);
            ApplyBool(section, nameof(DocumentExpiryCheckOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(DocumentExpiryCheckOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyTimeSpan(section, nameof(DocumentExpiryCheckOptions.PollInterval), value => opts.PollInterval = value);
            ApplyInt(section, nameof(DocumentExpiryCheckOptions.ExpiringSoonWindowDays), value => opts.ExpiringSoonWindowDays = value);
        });
        services.Configure<ProviderAutoAcceptOptions>(opts =>
        {
            var section = configuration.GetSection(ProviderAutoAcceptOptions.SectionName);
            ApplyBool(section, nameof(ProviderAutoAcceptOptions.Enabled), value => opts.Enabled = value);
            ApplyTimeSpan(section, nameof(ProviderAutoAcceptOptions.InitialDelay), value => opts.InitialDelay = value);
            ApplyTimeSpan(section, nameof(ProviderAutoAcceptOptions.PollInterval), value => opts.PollInterval = value);
            ApplyTimeSpan(section, nameof(ProviderAutoAcceptOptions.AutoAcceptAfter), value => opts.AutoAcceptAfter = value);
            ApplyInt(section, nameof(ProviderAutoAcceptOptions.BatchSize), value => opts.BatchSize = value);
        });
        services.AddHostedService<SlotLockCleanupService>();
        services.AddHostedService<DocumentExpiryCheckService>();
        services.AddHostedService<ProviderAutoAcceptService>();

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

    private static void ApplyTimeSpan(IConfiguration section, string key, Action<TimeSpan> apply)
    {
        if (TimeSpan.TryParse(section[key], out var value) && value > TimeSpan.Zero)
        {
            apply(value);
        }
    }
}
