using Booking.Application.Interfaces;
using Booking.Domain.Extensions;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices.Options;
using Booking.Infrastructure.Diagnostics;
using Booking.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class DocumentExpiryCheckService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<DocumentExpiryCheckOptions> options,
    IBookingBackgroundServiceStatusStore statusStore,
    ILogger<DocumentExpiryCheckService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private static readonly string ServiceName = nameof(DocumentExpiryCheckService);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var snapshot = options.CurrentValue;
        if (!snapshot.Enabled)
        {
            logger.LogInformation("{Service} disabled via config; skipping execution loop", ServiceName);
            return;
        }

        logger.LogInformation(
            "{Service} started; daily target = {Target} UTC",
            ServiceName,
            snapshot.TargetUtcTime);

        try
        {
            await Task.Delay(snapshot.InitialDelay, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var current = options.CurrentValue;
            var nextRun = SchedulingHelpers.NextOccurrenceUtc(current.TargetUtcTime, timeProvider);
            var delay = nextRun - timeProvider.GetUtcNow();
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            logger.LogDebug(
                "{Service}: next run at {NextRun:o} (in {Delay})",
                ServiceName,
                nextRun,
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            await TickAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var nowUtc = timeProvider.GetUtcNow();
        statusStore.RecordTickStart(ServiceName, nowUtc);
        BookingDiagnostics.BgServiceTicks.Add(1, new KeyValuePair<string, object?>("service", ServiceName));
        using var activity = BookingDiagnostics.ActivitySource.StartActivity(ServiceName);

        try
        {
            var processed = await RunOnceAsync(ct).ConfigureAwait(false);
            statusStore.RecordSuccess(ServiceName, timeProvider.GetUtcNow(), processed);
            if (processed > 0)
            {
                BookingDiagnostics.BgServiceItemsProcessed.Add(
                    processed,
                    new KeyValuePair<string, object?>("service", ServiceName));
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            statusStore.RecordFailure(ServiceName, timeProvider.GetUtcNow(), ex.Message);
            BookingDiagnostics.BgServiceFailures.Add(
                1,
                new KeyValuePair<string, object?>("service", ServiceName),
                new KeyValuePair<string, object?>("outcome", "failure"));
            logger.LogError(ex, "{Service} tick failed; will retry on next scheduled occurrence", ServiceName);
        }
    }

    internal async Task<int> RunOnceAsync(CancellationToken ct)
    {
        var snapshot = options.CurrentValue;
        var batchSize = snapshot.BatchSize > 0 ? snapshot.BatchSize : 1000;
        var windowDays = snapshot.ExpiringSoonWindowDays > 0 ? snapshot.ExpiringSoonWindowDays : 30;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var expiringThresholdUtc = nowUtc.AddDays(windowDays);

        using var scope = scopeFactory.CreateScope();
        var documentRepo = scope.ServiceProvider.GetRequiredService<IProviderDocumentRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IBookingUnitOfWork>();

        var expiredDocuments = await documentRepo
            .GetNewlyExpiredAsync(nowUtc, ct)
            .ConfigureAwait(false);

        var expiredCount = 0;
        foreach (var document in expiredDocuments)
        {
            if (expiredCount >= batchSize)
            {
                break;
            }

            document.MarkExpired(nowUtc);
            expiredCount++;
        }

        var expiringDocuments = await documentRepo
            .GetExpiringSoonAsync(nowUtc, expiringThresholdUtc, ct)
            .ConfigureAwait(false);

        var expiringCount = 0;
        foreach (var document in expiringDocuments)
        {
            if (expiringCount >= batchSize)
            {
                break;
            }

            document.MarkExpiring(nowUtc);
            expiringCount++;
        }

        var criticalPendingSuspension = await documentRepo
            .GetExpiredCriticalPendingSuspensionAsync(
                DocumentTypeExtensions.AllCritical,
                batchSize,
                ct)
            .ConfigureAwait(false);

        var suspendedCount = 0;
        foreach (var document in criticalPendingSuspension)
        {
            if (suspendedCount >= batchSize)
            {
                break;
            }

            document.MarkSuspensionDispatched(nowUtc);
            suspendedCount++;
        }

        var totalChanged = expiredCount + expiringCount + suspendedCount;
        if (totalChanged > 0)
        {
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var saturated = expiredDocuments.Count >= batchSize
            || expiringDocuments.Count >= batchSize
            || criticalPendingSuspension.Count >= batchSize;

        if (saturated)
        {
            logger.LogWarning(
                "{Service}: batch saturated (expired={Expired}, expiring={Expiring}, suspended={Suspended}, batchSize={BatchSize}); will resume next tick",
                ServiceName,
                expiredDocuments.Count,
                expiringDocuments.Count,
                criticalPendingSuspension.Count,
                batchSize);
        }
        else if (totalChanged > 0)
        {
            logger.LogInformation(
                "{Service}: processed {ExpiredCount} expired, {ExpiringCount} expiring, {SuspendedCount} suspended ProviderDocument(s)",
                ServiceName,
                expiredCount,
                expiringCount,
                suspendedCount);
        }
        else
        {
            logger.LogDebug("{Service}: no documents required notification", ServiceName);
        }

        return totalChanged;
    }
}
