using Analytics.Application.Interfaces.Repositories;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Analytics.Infrastructure.BackgroundServices;

/// <summary>
/// Daily job: executes matured GDPR deletion requests (30-day window passed)
/// and anonymizes interaction records older than 365 days.
/// </summary>
public sealed class GdprCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<GdprCleanupJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); // startup delay

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
                var gdprRepo = scope.ServiceProvider.GetRequiredService<IGdprDeletionRequestRepository>();
                var now = DateTime.UtcNow;

                // 1. Execute matured deletion requests
                var readyRequests = await gdprRepo.GetReadyForExecutionAsync(now, stoppingToken);
                foreach (var request in readyRequests)
                {
                    logger.LogInformation("Executing GDPR deletion for user {UserId}", request.UserId);

                    // Hard-delete user interactions
                    await context.UserInteractions
                        .Where(i => i.UserId == request.UserId)
                        .ExecuteDeleteAsync(stoppingToken);

                    // Hard-delete preferences
                    await context.UserPreferences
                        .Where(p => p.UserId == request.UserId)
                        .ExecuteDeleteAsync(stoppingToken);

                    // Hard-delete preferred categories
                    await context.UserPreferredCategories
                        .Where(c => c.UserId == request.UserId)
                        .ExecuteDeleteAsync(stoppingToken);

                    // Hard-delete excluded entities
                    await context.UserExcludedEntities
                        .Where(e => e.UserId == request.UserId)
                        .ExecuteDeleteAsync(stoppingToken);

                    // Hard-delete experiment assignments
                    await context.Set<Domain.Entities.ExperimentAssignment>()
                        .Where(a => a.UserId == request.UserId)
                        .ExecuteDeleteAsync(stoppingToken);

                    // Anonymize suggestion metrics
                    await context.Set<Domain.Entities.SuggestionMetric>()
                        .Where(m => m.UserId == request.UserId)
                        .ExecuteUpdateAsync(s => s.SetProperty(m => m.UserId, (Guid?)null), stoppingToken);

                    // Anonymize recommendation cache
                    await context.RecommendationCaches
                        .Where(r => r.UserId == request.UserId)
                        .ExecuteDeleteAsync(stoppingToken);

                    request.MarkExecuted();
                    await context.SaveChangesAsync(stoppingToken);

                    logger.LogInformation("GDPR deletion completed for user {UserId}", request.UserId);
                }

                // 2. Daily anonymization: null out UserId on interactions older than 365 days
                var cutoff = now.AddDays(-365);
                var anonymized = await context.UserInteractions
                    .Where(i => i.UserId != null && i.OccurredAt < cutoff)
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.UserId, (Guid?)null), stoppingToken);

                if (anonymized > 0)
                    logger.LogInformation("Anonymized {Count} interaction records older than 365 days", anonymized);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "GDPR cleanup job failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
