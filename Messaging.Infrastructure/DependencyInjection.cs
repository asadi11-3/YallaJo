using MediatR;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace Messaging.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMessagingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<MessagingDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "messaging");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<MessagingDbContext>, UnitOfWork<MessagingDbContext>>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddHostedService<OutboxProcessor<MessagingDbContext>>();

        return services;
    }
}
