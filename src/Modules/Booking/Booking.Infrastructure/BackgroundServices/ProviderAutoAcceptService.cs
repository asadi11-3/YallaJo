using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices.Options;
using Booking.Infrastructure.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class ProviderAutoAcceptService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<ProviderAutoAcceptOptions> options,
    IBookingBackgroundServiceStatusStore statusStore,
    ILogger<ProviderAutoAcceptService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private static readonly string ServiceName = nameof(ProviderAutoAcceptService);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var snapshot = options.CurrentValue;
        if (!snapshot.Enabled)
        {
            logger.LogInformation("{Service} disabled via config; skipping execution loop", ServiceName);
            return;
        }

        var interval = snapshot.Interval > TimeSpan.Zero ? snapshot.Interval : TimeSpan.FromMinutes(15);
        logger.LogInformation(
            "{Service} started with interval {Interval} (ProviderConfirmationHours={Hours})",
            ServiceName,
            interval,
            snapshot.ProviderConfirmationHours);

        try
        {
            await Task.Delay(snapshot.InitialDelay, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using var timer = new PeriodicTimer(interval);
        do
        {
            await TickAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
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
            logger.LogError(ex, "{Service} tick failed; will retry next interval", ServiceName);
        }
    }

    internal async Task<int> RunOnceAsync(CancellationToken ct)
    {
        var snapshot = options.CurrentValue;
        var batchSize = snapshot.BatchSize > 0 ? snapshot.BatchSize : 500;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var cutoffUtc = nowUtc.Subtract(snapshot.ConfirmationWindow);

        using var scope = scopeFactory.CreateScope();
        var bookingRepo = scope.ServiceProvider.GetRequiredService<ITourBookingRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IBookingUnitOfWork>();

        var stuck = await bookingRepo
            .GetPendingConfirmationOlderThanAsync(cutoffUtc, batchSize, ct)
            .ConfigureAwait(false);

        if (stuck.Count == 0)
        {
            logger.LogDebug("{Service}: no PendingConfirmation bookings older than cutoff", ServiceName);
            return 0;
        }

        var confirmedCount = 0;
        foreach (var booking in stuck)
        {
            try
            {
                booking.Confirm(ConfirmationSource.AutoAccept);
                confirmedCount++;
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogInformation(
                    "{Service}: skipping booking {BookingId} (state guard rejected auto-confirm): {Reason}",
                    ServiceName,
                    booking.Id,
                    ex.Message);
            }
        }

        if (confirmedCount > 0)
        {
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (stuck.Count >= batchSize)
        {
            logger.LogWarning(
                "{Service}: batch saturated ({Count}/{BatchSize}); investigate PendingConfirmation backlog",
                ServiceName,
                stuck.Count,
                batchSize);
        }
        else if (confirmedCount > 0)
        {
            logger.LogInformation(
                "{Service}: auto-confirmed {Count} pending provider booking(s)",
                ServiceName,
                confirmedCount);
        }

        return confirmedCount;
    }
}
