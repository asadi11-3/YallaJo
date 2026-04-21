using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Security.Application.Interfaces;
using Security.Contracts.Abstractions;
using Security.Domain.Repositories;
using Security.Infrastructure.Persistence;
using Security.Infrastructure.Persistence.Seeding;
using Security.Infrastructure.Repositories;
using Security.Contracts.Authorization;
using Security.Infrastructure.Seeding;
using Security.Infrastructure.Services;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Security.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<SecurityDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "security");
                    sql.EnableRetryOnFailure(3);
                }));

        services.AddScoped<IUnitOfWork<SecurityDbContext>, UnitOfWork<SecurityDbContext>>();
        services.AddScoped<IModuleDbInitializer, SecurityDbInitializer>();
        services.AddScoped<ISecurityUnitOfWork, SecurityUnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ISecurityInboxStore, SecurityInboxStore>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRoleClaimRepository, RoleClaimRepository>();
        services.AddScoped<IUserClaimRepository, UserClaimRepository>();
        services.AddScoped<SecurityDataSeeder>();
        services.AddScoped<RolePermissionMapping>();
        services.AddSingleton<IPermissionCatalog, SecurityPermissionCatalog>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

     
        services.AddScoped<ISecurityService, SecurityService>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<SecurityDbContext>>();

        return services;
    }
}
