using Finance.Application.Interfaces;
using Finance.Contracts.Authorization;
using Finance.Contracts.Services;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using Finance.Infrastructure.Persistence.Seeding;
using Finance.Infrastructure.Repositories;
using Finance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;
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
        services.AddScoped<IInvoiceItemRepository, InvoiceItemRepository>();
        services.AddScoped<IDisputeRepository, DisputeRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IFinanceOutboxWriter, FinanceOutboxWriter>();

        // ── Permission catalog ─────────────────────────────────────────────
        services.AddSingleton<IPermissionCatalog, FinancePermissionCatalog>();

        // ── Cross-module read-only services ──────────────────────────────────
        services.AddScoped<ICommissionLookupService, CommissionLookupService>();

        // ── Payment gateway ─────────────────────────────────────────────────
        // PCI-DSS: real gateway implementation must be selected by environment.
        // For now we register the fake gateway. Production wiring is the responsibility
        // of the Web composition root once a real provider (Stripe, PayPal) is chosen.
        services.AddScoped<IPaymentGateway, FakePaymentGateway>();

        return services;
    }
}
