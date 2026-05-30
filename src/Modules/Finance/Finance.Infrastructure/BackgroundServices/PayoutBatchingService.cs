using Finance.Application.Commands.TriggerPayout;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finance.Infrastructure.BackgroundServices;

/// <summary>
/// T5: weekly payout sweep at Sunday midnight UTC.
/// Calls the same MediatR <see cref="TriggerPayoutCommand"/> as the admin trigger endpoint (DRY).
/// </summary>
public sealed class PayoutBatchingService(
    IServiceScopeFactory scopeFactory,
    IOptions<PayoutBatchingOptions> options,
    TimeProvider timeProvider,
    ILogger<PayoutBatchingService> logger) : BackgroundService
{
    private readonly PayoutBatchingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("PayoutBatchingService disabled by config; exiting.");
            return;
        }

        logger.LogInformation(
            "PayoutBatchingService started. Target: every {Day} at {Time} UTC.",
            _options.TargetDayOfWeek, _options.TargetTimeUtc);

        try
        {
            var initialDelay = ComputeDelayUntilNext(timeProvider.GetUtcNow().UtcDateTime);
            logger.LogInformation("Next payout batch in {Delay}.", initialDelay);
            await Task.Delay(initialDelay, stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromDays(7));
            do
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "PayoutBatchingService tick failed; will retry on next weekly schedule.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }

        logger.LogInformation("PayoutBatchingService stopped.");
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new TriggerPayoutCommand(IsManual: false), ct);
        if (result.IsSuccess)
        {
            logger.LogInformation(
                "PayoutBatchingService tick completed. Created={Created}, Skipped={Skipped}, OnHold={OnHold}, Total={Total}.",
                result.Value.CreatedCount,
                result.Value.SkippedCount,
                result.Value.OnHoldCount,
                result.Value.TotalSweptAmount);
        }
        else
        {
            logger.LogWarning("PayoutBatchingService tick failed: {Error}", result.Error.Message);
        }
    }

    private TimeSpan ComputeDelayUntilNext(DateTime nowUtc)
    {
        var todayAtTarget = new DateTime(
            nowUtc.Year, nowUtc.Month, nowUtc.Day,
            _options.TargetTimeUtc.Hours, _options.TargetTimeUtc.Minutes, 0,
            DateTimeKind.Utc);

        var days = ((int)_options.TargetDayOfWeek - (int)nowUtc.DayOfWeek + 7) % 7;
        var next = todayAtTarget.AddDays(days);

        if (next <= nowUtc)
        {
            next = next.AddDays(7);
        }

        return next - nowUtc;
    }
}

public sealed class PayoutBatchingOptions
{
    public const string SectionName = "Finance:BackgroundServices:PayoutBatching";
    public bool Enabled { get; set; } = true;
    public DayOfWeek TargetDayOfWeek { get; set; } = DayOfWeek.Sunday;
    public TimeSpan TargetTimeUtc { get; set; } = TimeSpan.Zero;
}
