using Auth.Domain.Repositories;
using System;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AuthConnection")
            ?? throw new InvalidOperationException("Connection string 'AuthConnection' is not configured.");

        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "auth");
                    sql.EnableRetryOnFailure(3);
                }));

        // UnitOfWork wraps AuthDbContext and dispatches domain events on SaveChanges
        services.AddScoped<IUnitOfWork<AuthDbContext>, UnitOfWork<AuthDbContext>>();

        // Repositories
        services.AddScoped<IUserCredentialsRepository, UserCredentialsRepository>();

        // MediatR — registers integration event handlers from this assembly
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
