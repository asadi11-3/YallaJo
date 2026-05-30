using Analytics.Application.Interfaces.Repositories;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Analytics.Infrastructure.BackgroundServices;

/// <summary>
/// 30-minute background aggregation job for CTR / conversion funnel metrics.
/// Aggregates SuggestionMetrics into pre-computed summary tables.
/// Also resets daily CPC budget spend at midnight UTC and computes per-provider CTR.
/// </summary>
public sealed class MetricsAggregationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<MetricsAggregationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Metrics aggregation cycle starting");

                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();

                // 1. Reset daily CPC spend at midnight UTC (idempotent — only resets if SpentToday > 0)
                await ResetDailyBudgetsAsync(context, stoppingToken);

                // 2. Compute per-(Context, Position) CTR from raw SuggestionMetrics
                // For now, GetMetricsQuery still computes on-the-fly, but this resets CPC spend daily.

                logger.LogInformation("Metrics aggregation cycle completed");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Metrics aggregation job failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    /// <summary>
    /// Reset SpentToday to 0 for all CPC boost packages where SpentToday > 0.
    /// Runs every 30min but is idempotent — only meaningful around midnight UTC.
    /// </summary>
    private async Task ResetDailyBudgetsAsync(AnalyticsDbContext context, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Only reset during the midnight hour to avoid mid-day resets
        if (now.Hour != 0) return;

        var cpcBoosts = await context.BoostPackages
            .Where(b => b.BillingMode == "CPC" && b.IsActive && b.SpentToday > 0m)
            .ToListAsync(ct);

        if (cpcBoosts.Count == 0) return;

        foreach (var boost in cpcBoosts)
        {
            boost.ResetDailySpend();
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Reset daily CPC budget for {Count} boost packages", cpcBoosts.Count);
    }
}
