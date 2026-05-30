using Accounts.Infrastructure.Persistence;
using Analytics.Infrastructure.Persistence;
using Auth.Infrastructure.Persistence;
using Booking.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Persistence;
using ContentCore.Infrastructure.Persistence;
using ContentPlaces.Infrastructure.Persistence;
using ContentSeo.Infrastructure.Persistence;
using ContentTours.Infrastructure.Persistence;
using Finance.Infrastructure.Persistence;
using Messaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Security.Infrastructure.Persistence;
using Social.Infrastructure.Persistence;
using Tracking.Infrastructure.Persistence;
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

            await ApplyMigrationsAsync(scope.ServiceProvider, logger, cancellationToken);

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

        private static async Task ApplyMigrationsAsync(
            IServiceProvider serviceProvider,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var contextTypes = new[]
            {
                typeof(AccountsDbContext),
                typeof(AuthDbContext),
                typeof(SecurityDbContext),
                typeof(ContentCoreDbContext),
                typeof(ContentPlacesDbContext),
                typeof(ContentToursDbContext),
                typeof(ContentBlogsDbContext),
                typeof(ContentSeoDbContext),
                typeof(AnalyticsDbContext),
                typeof(BookingDbContext),
                typeof(FinanceDbContext),
                typeof(MessagingDbContext),
                typeof(SocialDbContext),
                typeof(TrackingDbContext)
            };

            foreach (var contextType in contextTypes)
            {
                if (serviceProvider.GetService(contextType) is not DbContext dbContext)
                {
                    continue;
                }

                try
                {
                    await dbContext.Database.MigrateAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Database migration failed for DbContext {DbContextType}. Seeding continues.",
                        contextType.FullName);
                }
            }
        }
    }
}
