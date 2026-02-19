
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Accounts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AccountsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "accounts");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<AccountsDbContext>, UnitOfWork<AccountsDbContext>>();

        return services;
    }
}