using MediatR;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Abstractions.Behaviors;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Application.Abstractions.Events;
using YallaJo.SharedKernel.Infrastructure.Clock;
using YallaJo.SharedKernel.Infrastructure.Events;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;
namespace YallaJo.SharedKernel.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers shared infrastructure services: domain event dispatcher, clock,
        /// and global MediatR pipeline behaviors.
        /// Call this ONCE from the host after all module registrations.
        /// </summary>
        public static IServiceCollection AddSharedKernelInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            });

            // Single composite outbox processor replaces per-module hosted services
            services.AddHostedService<CompositeOutboxProcessor>();
            return services;
        }
    }
}
