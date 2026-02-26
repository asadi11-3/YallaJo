using Finance.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Finance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFinanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
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
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
