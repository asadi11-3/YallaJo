using Accounts.Domain.Repositories;
using Accounts.Infrastructure.BackgroundServices;
using Accounts.Infrastructure.Persistence;
using Accounts.Infrastructure.Persistence.Seeding;
using Accounts.Infrastructure.Repositories;
using Accounts.Application.Interfaces;
using Accounts.Contracts.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.Outbox;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Accounts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AccountsDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "accounts");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<AccountsDbContext>, UnitOfWork<AccountsDbContext>>();
        services.AddSingleton<IPermissionCatalog, AccountsPermissionCatalog>();
        services.AddScoped<IModuleDbInitializer, AccountsDbInitializer>();
        services.AddScoped<IAccountsUnitOfWork, AccountsUnitOfWork>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IProviderApplicationRepository, ProviderApplicationRepository>();
        services.AddScoped<IAgencyAffiliationRepository, AgencyAffiliationRepository>();
        services.AddScoped<IAgencyInvitationRepository, AgencyInvitationRepository>();
        services.AddScoped<IAgencyApplicationRepository, AgencyApplicationRepository>();
        services.AddScoped<IAccountsOutboxWriter, AccountsOutboxWriter>();
        services.AddScoped<IAccountsInboxStore, AccountsInboxStore>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<AccountsDbContext>>();
        services.AddScoped<IOutboxCleaner, OutboxCleaner<AccountsDbContext>>();

        // Background services
        services.Configure<AgencyInvitationExpiryOptions>(
            configuration.GetSection(AgencyInvitationExpiryOptions.SectionName));
        services.Configure<ProviderDocumentExpiryOptions>(
            configuration.GetSection(ProviderDocumentExpiryOptions.SectionName));
        services.AddHostedService<AgencyInvitationExpiryService>();
        services.AddHostedService<ProviderDocumentExpiryService>();

        // Distributed cache — idempotent, safe if host already registered it
        services.AddHybridCache();

        return services;
    }
}
