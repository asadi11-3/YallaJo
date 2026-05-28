using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices.Options;
using Booking.Infrastructure.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class BookingAutoExpireService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<BookingAutoExpireOptions> options,
    IBookingBackgroundServiceStatusStore statusStore,
    ILogger<BookingAutoExpireService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private static readonly string ServiceName = nameof(BookingAutoExpireService);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var snapshot = options.CurrentValue;
        if (!snapshot.Enabled)
        {
            logger.LogInformation("{Service} disabled via config; skipping execution loop", ServiceName);
            return;
        }

        var interval = snapshot.Interval > TimeSpan.Zero ? snapshot.Interval : TimeSpan.FromMinutes(5);
        logger.LogInformation(
            "{Service} started with interval {Interval} (PaymentWindowMinutes={Window})",
            ServiceName,
            interval,
            snapshot.PaymentWindowMinutes);

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

        using var scope = scopeFactory.CreateScope();
        var bookingRepo = scope.ServiceProvider.GetRequiredService<ITourBookingRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IBookingUnitOfWork>();

        var stuck = await bookingRepo
            .GetExpiredAwaitingPaymentAsync(nowUtc, batchSize, ct)
            .ConfigureAwait(false);

        if (stuck.Count == 0)
        {
            logger.LogDebug("{Service}: no AwaitingPayment bookings past PaymentExpiresAt", ServiceName);
            return 0;
        }

        var expiredCount = 0;
        foreach (var booking in stuck)
        {
            try
            {
                booking.MoveToAwaitingPaymentExpired();
                expiredCount++;
            }
            catch (BusinessRuleViolationException ex)
            {
                logger.LogInformation(
                    "{Service}: skipping booking {BookingId} (state guard rejected expire): {Reason}",
                    ServiceName,
                    booking.Id,
                    ex.Message);
            }
        }

        if (expiredCount > 0)
        {
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (stuck.Count >= batchSize)
        {
            logger.LogWarning(
                "{Service}: batch saturated ({Count}/{BatchSize}); investigate AwaitingPayment backlog",
                ServiceName,
                stuck.Count,
                batchSize);
        }
        else if (expiredCount > 0)
        {
            logger.LogInformation(
                "{Service}: expired {Count} AwaitingPayment booking(s)",
                ServiceName,
                expiredCount);
        }

        return expiredCount;
    }
}
