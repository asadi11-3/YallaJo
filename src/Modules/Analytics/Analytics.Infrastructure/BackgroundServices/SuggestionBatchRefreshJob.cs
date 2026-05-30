using Analytics.Application.Commands.RefreshSuggestionBatch;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Analytics.Infrastructure.BackgroundServices;

internal sealed class SuggestionBatchRefreshJob(
    IServiceScopeFactory scopeFactory,
    IOptions<SuggestionBatchRefreshOptions> options,
    ILogger<SuggestionBatchRefreshJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, settings.StartupDelaySeconds)), stoppingToken);

        await BootstrapEntityAttributeSnapshotsAsync(stoppingToken);
        await RefreshStaleBatchesAsync(settings, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, settings.RefreshIntervalMinutes)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshStaleBatchesAsync(settings, stoppingToken);
        }
    }

    private async Task BootstrapEntityAttributeSnapshotsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        var snapshotCount = await db.EntityAttributeSnapshots.CountAsync(ct).ConfigureAwait(false);

        if (snapshotCount == 0)
        {
            logger.LogWarning("No entity snapshots found. Snapshots are populated via integration events from Content modules. Ensure Content modules are running and publishing events.");
        }
    }

    private async Task RefreshStaleBatchesAsync(SuggestionBatchRefreshOptions settings, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var batches = scope.ServiceProvider.GetRequiredService<ISuggestionBatchRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var stale = await batches.GetStaleAsync(settings.MaxBatchesPerCycle, ct).ConfigureAwait(false);
        foreach (var batch in stale)
        {
            try
            {
                await sender.Send(new RefreshSuggestionBatchCommand(batch.SourceKind, batch.SourceId, batch.Context), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh suggestion batch {BatchId} for {SourceKind}/{SourceId} context {Context}", batch.Id, batch.SourceKind, batch.SourceId, batch.Context);
            }
        }

        if (stale.Count > 0)
            logger.LogInformation("Processed {Count} stale suggestion batches", stale.Count);
    }
}
