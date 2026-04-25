using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

/// <summary>
/// Single hosted service that deletes successfully processed outbox rows
/// older than the configured retention window across ALL modules.
///
/// Runs every <see cref="OutboxCleanupOptions.CleanupInterval"/> (default 1 hour).
/// An initial 1-minute startup delay ensures DbContexts are ready.
///
/// NEVER deletes dead-lettered rows — those require manual investigation.
///
/// Per-module: resolves every <see cref="IOutboxCleaner"/> from DI
/// (one per module DbContext) and iterates sequentially.
///
/// Opt-out: set <c>OutboxCleanup:Enabled = false</c> in appsettings.
/// </summary>
public sealed class OutboxCleanupBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<OutboxCleanupBackgroundService> logger,
    IOptions<OutboxCleanupOptions> options) : BackgroundService
{
    private readonly OutboxCleanupOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Outbox cleanup service is DISABLED by configuration");
            return;
        }

        logger.LogInformation(
            "Outbox cleanup service started — retention: {Retention}, interval: {Interval}, batch: {Batch}",
            _options.RetentionPeriod, _options.CleanupInterval, _options.BatchSize);

        // 1-minute startup delay so DbContexts and migrations finish first
        try { await Task.Delay(TimeSpan.FromMinutes(1), ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CleanupAllModulesAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox cleanup iteration failed unexpectedly");
            }

            try { await Task.Delay(_options.CleanupInterval, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }

        logger.LogInformation("Outbox cleanup service stopped");
    }

    private async Task CleanupAllModulesAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var cleaners = scope.ServiceProvider.GetServices<IOutboxCleaner>().ToList();

        if (cleaners.Count == 0) return;

        var cutoff = DateTime.UtcNow - _options.RetentionPeriod;

        logger.LogDebug(
            "Outbox cleanup starting — {ModuleCount} modules, cutoff {Cutoff:u}",
            cleaners.Count, cutoff);

        int totalDeleted = 0;

        foreach (var cleaner in cleaners)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var deleted = await cleaner.DeleteProcessedBeforeAsync(cutoff, _options.BatchSize, ct);

                if (deleted > 0)
                {
                    logger.LogInformation(
                        "Outbox cleanup: deleted {Count} row(s) for {Module}",
                        deleted, cleaner.ModuleName);

                    totalDeleted += deleted;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                // One module's failure does not block cleanup for other modules
                logger.LogError(ex, "Outbox cleanup failed for {Module}", cleaner.ModuleName);
            }
        }

        if (totalDeleted > 0)
        {
            logger.LogInformation(
                "Outbox cleanup complete — {Total} total row(s) deleted across {ModuleCount} module(s)",
                totalDeleted, cleaners.Count);
            OutboxMetrics.CleanupDeletedTotal.Add(totalDeleted);
        }
    }
}
