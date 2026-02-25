using Accounts.Domain.Repositories;
using Accounts.Infrastructure.Persistence;
using Accounts.Infrastructure.Repositories;
using Accounts.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.Inbox;

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
        services.AddScoped<IAccountsUnitOfWork, AccountsUnitOfWork>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IAccountsInboxStore, AccountsInboxStore>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddHostedService<OutboxProcessor<AccountsDbContext>>();

        return services;
    }
}
