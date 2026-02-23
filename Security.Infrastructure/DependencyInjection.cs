using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using Security.Infrastructure.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Security.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SecurityConnection")
            ?? throw new InvalidOperationException("Connection string 'SecurityConnection' is not configured.");

        services.AddDbContext<SecurityDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "security");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<SecurityDbContext>, UnitOfWork<SecurityDbContext>>();
        services.AddScoped<ISecurityUnitOfWork, SecurityUnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISecurityUserExistenceChecker, Services.SecurityUserExistenceChecker>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddHostedService<OutboxProcessor<SecurityDbContext>>();

        return services;
    }
}
