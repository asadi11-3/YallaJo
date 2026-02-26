using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace Social.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSocialInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<SocialDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "social");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<SocialDbContext>, UnitOfWork<SocialDbContext>>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddHostedService<OutboxProcessor<SocialDbContext>>();

        return services;
    }
}
