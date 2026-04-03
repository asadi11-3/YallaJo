using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using ContentPlaces.Infrastructure.Persistence.Seeding;
using ContentPlaces.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentPlaces.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddContentPlacesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ContentPlacesDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_places");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<ContentPlacesDbContext>, UnitOfWork<ContentPlacesDbContext>>();
        services.AddScoped<IContentPlacesUnitOfWork, ContentPlacesUnitOfWork>();
        services.AddScoped<IContentPlacesInboxStore, ContentPlacesInboxStore>();
        services.AddScoped<IModuleDbInitializer, ContentPlacesDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentPlacesDbContext>>();
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        return services;
    }
}
