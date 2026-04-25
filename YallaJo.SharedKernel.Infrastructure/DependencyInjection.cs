using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Abstractions.Behaviors;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Application.Abstractions.Events;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
using YallaJo.SharedKernel.Infrastructure.Clock;
using YallaJo.SharedKernel.Infrastructure.Events;
using YallaJo.SharedKernel.Infrastructure.Outbox;
namespace YallaJo.SharedKernel.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers shared infrastructure services: domain event dispatcher, clock,
        /// and global MediatR pipeline behaviors.
        /// Call this ONCE from the host after all module registrations.
        /// </summary>
        public static IServiceCollection AddSharedKernelInfrastructure(
            this IServiceCollection services,
            IConfiguration? configuration = null)
        {
            services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
            services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(15),
                    LocalCacheExpiration = TimeSpan.FromMinutes(5),
                };
                options.MaximumPayloadBytes = 1024 * 1024;
            });
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
                cfg.AddOpenBehavior(typeof(QueryCachingBehavior<,>));
            });

            // Single composite outbox processor replaces per-module hosted services
            services.AddHostedService<CompositeOutboxProcessor>();

            // ── Outbox cleanup (P0 hardening) ────────────────────────────────────────
            // Binds OutboxCleanup section from appsettings if configuration is provided.
            // Falls back to defaults (30-day retention, 1-hour interval) if section absent.
            if (configuration is not null)
                services.Configure<OutboxCleanupOptions>(o =>
                {
                    var section = configuration.GetSection("OutboxCleanup");
                    var enabled = section["Enabled"];
                    if (enabled is not null && bool.TryParse(enabled, out var e)) o.Enabled = e;

                    var retention = section["RetentionPeriod"];
                    if (retention is not null && TimeSpan.TryParse(retention, out var r)) o.RetentionPeriod = r;

                    var interval = section["CleanupInterval"];
                    if (interval is not null && TimeSpan.TryParse(interval, out var i)) o.CleanupInterval = i;

                    var batch = section["BatchSize"];
                    if (batch is not null && int.TryParse(batch, out var b)) o.BatchSize = b;
                });
            else
                services.Configure<OutboxCleanupOptions>(_ => { }); // use defaults

            services.AddHostedService<OutboxCleanupBackgroundService>();

            return services;
        }
    }
}
