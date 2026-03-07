using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using ContentSeo.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace ContentSeo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddContentSeoInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ContentSeoDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_seo");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<ContentSeoDbContext>, UnitOfWork<ContentSeoDbContext>>();
        services.AddScoped<IContentSeoUnitOfWork, ContentSeoUnitOfWork>();
        services.AddScoped<IContentSeoInboxStore, ContentSeoInboxStore>();
        services.AddScoped<IModuleDbInitializer, ContentSeoDbInitializer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<ContentSeoDbContext>>();

        return services;
    }
}
