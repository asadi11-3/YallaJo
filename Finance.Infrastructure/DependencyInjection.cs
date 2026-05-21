using Finance.Application.Commands.TriggerPayout;
using Finance.Application.EventHandlers;
using Finance.Application.Interfaces;
using Finance.Contracts.Authorization;
using Finance.Contracts.Services;
using Finance.Domain.Repositories;
using Finance.Infrastructure.BackgroundServices;
using Finance.Infrastructure.Gateways;
using Finance.Infrastructure.Pdf;
using Finance.Infrastructure.Persistence;
using Finance.Infrastructure.Persistence.Seeding;
using Finance.Infrastructure.Repositories;
using Finance.Infrastructure.Services;
using Finance.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Finance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFinanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<FinanceDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "finance");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<FinanceDbContext>, UnitOfWork<FinanceDbContext>>();
        services.AddScoped<IFinanceUnitOfWork, FinanceUnitOfWork>();
        services.AddScoped<IModuleDbInitializer, FinanceDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<FinanceDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<FinanceDbContext>>();

        // ── Repositories ────────────────────────────────────────────────────
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPayoutRepository, PayoutRepository>();
        services.AddScoped<IPayoutItemRepository, PayoutItemRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<ICommissionRuleRepository, CommissionRuleRepository>();
        services.AddScoped<IProviderBankAccountRepository, ProviderBankAccountRepository>();
        services.AddScoped<IPaymentExpectationRepository, PaymentExpectationRepository>();
        services.AddScoped<IDisputeRepository, DisputeRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IFinanceOutboxWriter, FinanceOutboxWriter>();

        // ── Inbox store ─────────────────────────────────────────────────────
        services.AddScoped<IFinanceInboxStore, FinanceInboxStore>();

        // ── Permission catalog ─────────────────────────────────────────────
        services.AddSingleton<IPermissionCatalog, FinancePermissionCatalog>();

        // ── Cross-module read-only services ──────────────────────────────────
        services.AddScoped<ICommissionLookupService, CommissionLookupService>();

        // ── Payment gateway ─────────────────────────────────────────────────
        // PCI-DSS: real gateway implementation must be selected by environment.
        // For now we register the fake gateway. Production wiring is the responsibility
        // of the Web composition root once a real provider (Stripe, PayPal) is chosen.
        services.Configure<FakePaymentGatewayOptions>(opts =>
        {
            var section = configuration.GetSection(FakePaymentGatewayOptions.SectionName);
            opts.Provider = section[nameof(FakePaymentGatewayOptions.Provider)] ?? opts.Provider;
            opts.WebhookSecret = section[nameof(FakePaymentGatewayOptions.WebhookSecret)] ?? opts.WebhookSecret;
            var hosts = section.GetSection(nameof(FakePaymentGatewayOptions.ReturnUrlAllowedHosts))
                .GetChildren()
                .Select(child => child.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray();
            if (hosts.Length > 0)
            {
                opts.ReturnUrlAllowedHosts = hosts;
            }
        });
        services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

        // ── Invoice rendering + storage (T3) ───────────────────────────────
        services.Configure<LocalFileInvoiceStorageOptions>(opts =>
        {
            var section = configuration.GetSection(LocalFileInvoiceStorageOptions.SectionName);
            var basePath = section["BasePath"];
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                opts.BasePath = basePath;
            }
        });
        services.AddScoped<IInvoiceNumberGenerator, SqlInvoiceNumberGenerator>();
        services.AddSingleton<IInvoicePdfRenderer, QuestPdfInvoiceRenderer>();
        services.AddSingleton<IInvoiceStorage, LocalFileInvoiceStorage>();
        QuestPdfInvoiceRenderer.ActivateCommunityLicense();

        // ── Payout (T4) configuration ───────────────────────────────────────
        services.Configure<EscrowOptions>(opts =>
        {
            var section = configuration.GetSection(EscrowOptions.SectionName);
            var holdDays = section["HoldDays"];
            if (int.TryParse(holdDays, out var d) && d > 0)
            {
                opts.HoldDays = d;
            }
        });
        services.Configure<TriggerPayoutOptions>(opts =>
        {
            var section = configuration.GetSection(TriggerPayoutOptions.SectionName);
            var min = section["MinPayoutThreshold"];
            var large = section["LargePayoutThreshold"];
            var hold = section["EscrowReleaseDays"];
            if (decimal.TryParse(min, out var minVal)) opts.MinPayoutThreshold = minVal;
            if (decimal.TryParse(large, out var largeVal)) opts.LargePayoutThreshold = largeVal;
            if (int.TryParse(hold, out var holdVal)) opts.EscrowReleaseDays = holdVal;
        });

        // ── Background services (T5) ────────────────────────────────────────
        services.Configure<PayoutBatchingOptions>(opts =>
        {
            var section = configuration.GetSection(PayoutBatchingOptions.SectionName);
            if (bool.TryParse(section["Enabled"], out var enabled)) opts.Enabled = enabled;
            if (Enum.TryParse<DayOfWeek>(section["TargetDayOfWeek"], out var day)) opts.TargetDayOfWeek = day;
            if (TimeSpan.TryParse(section["TargetTimeUtc"], out var time)) opts.TargetTimeUtc = time;
        });
        services.Configure<RefundRetryOptions>(opts =>
        {
            var section = configuration.GetSection(RefundRetryOptions.SectionName);
            if (bool.TryParse(section["Enabled"], out var enabled)) opts.Enabled = enabled;
            if (TimeSpan.TryParse(section["InitialDelay"], out var init)) opts.InitialDelay = init;
            if (TimeSpan.TryParse(section["Period"], out var per)) opts.Period = per;
            if (TimeSpan.TryParse(section["Cooloff"], out var cool)) opts.Cooloff = cool;
            if (int.TryParse(section["MaxRetries"], out var max)) opts.MaxRetries = max;
            if (int.TryParse(section["BatchSize"], out var bs)) opts.BatchSize = bs;
        });
        services.AddHostedService<PayoutBatchingService>();
        services.AddHostedService<RefundRetryService>();

        return services;
    }
}
