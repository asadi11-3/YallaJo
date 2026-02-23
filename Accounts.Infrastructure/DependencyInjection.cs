using Accounts.Domain.Interfaces;
using System;
using Accounts.Infrastructure.Persistence;
using Accounts.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Accounts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AccountsConnection")
            ?? throw new InvalidOperationException("Connection string 'AccountsConnection' is not configured.");

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
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddHostedService<OutboxProcessor<AccountsDbContext>>();

        return services;
    }
}
