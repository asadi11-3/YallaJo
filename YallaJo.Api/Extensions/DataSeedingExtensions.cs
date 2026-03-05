using YallaJo.SharedKernel.Infrastructure.Data;

namespace YallaJo.Api.Extensions
{
    public static class DataSeedingExtensions
    {
        public static IServiceCollection AddDataSeeding(this IServiceCollection services)
        {
            return services;
        }

        public static async Task UseDataSeedingAsync(this WebApplication app, CancellationToken cancellationToken = default)
        {
            var seedingEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Seeding:Enabled");

            if (!seedingEnabled)
            {
                return;
            }

            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DataSeeding");

            var initializers = scope.ServiceProvider
                .GetServices<IModuleDbInitializer>()
                .OrderBy(initializer => initializer.Order)
                .ThenBy(initializer => initializer.GetType().FullName)
                .ToList();

            if (initializers.Count == 0)
            {
                logger.LogInformation("No module data initializers registered.");
                return;
            }

            var failures = 0;

            foreach (var initializer in initializers)
            {
                try
                {
                    await initializer.InitializeAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    failures++;
                    logger.LogError(
                        ex,
                        "Data seeding failed in initializer {Initializer}. Startup will continue.",
                        initializer.GetType().FullName);
                }
            }

            logger.LogInformation(
                "Data seeding finished. Initializers: {InitializerCount}, Failures: {FailureCount}",
                initializers.Count,
                failures);
        }
    }
}
