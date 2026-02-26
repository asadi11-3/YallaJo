using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.Data;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace ContentBlogs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddContentBlogsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ContentBlogsDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "content_blogs");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<ContentBlogsDbContext>, UnitOfWork<ContentBlogsDbContext>>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddHostedService<OutboxProcessor<ContentBlogsDbContext>>();

        return services;
    }
}
