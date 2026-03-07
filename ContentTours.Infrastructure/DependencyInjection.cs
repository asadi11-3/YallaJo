using ContentTours.Application.Interfaces;
using ContentTours.Infrastructure.Persistence;
using ContentTours.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace ContentTours.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddContentToursInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ContentToursDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_tours");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<ContentToursDbContext>, UnitOfWork<ContentToursDbContext>>();
        services.AddScoped<IContentToursUnitOfWork, ContentToursUnitOfWork>();
        services.AddScoped<IContentToursInboxStore, ContentToursInboxStore>();
        services.AddScoped<IModuleDbInitializer, ContentToursDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentToursDbContext>>();

        return services;
    }
}
