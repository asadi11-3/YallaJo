using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.Infrastructure.BackgroundServices;

public sealed class NotificationDigestOptions
{
    public const string SectionName = "Messaging:BackgroundServices:NotificationDigest";

    public bool Enabled { get; init; } = false; // Disabled until DigestBatchId/DigestFrequency schema is added
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMinutes(10);
    public int BatchSize { get; init; } = 200;
}

/// <summary>
/// Periodically batches pending digest notifications and dispatches them as grouped emails or in-app summaries.
/// Currently a no-op stub — requires Notification.DigestFrequency, IsDigestDispatched, DigestScheduledFor
/// schema columns before activation. Set Messaging:BackgroundServices:NotificationDigest:Enabled=true
/// after the EF migration is applied.
/// </summary>
internal sealed class NotificationDigestService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<NotificationDigestOptions> options,
    ILogger<NotificationDigestService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation(
                "Notification digest service is disabled. " +
                "Enable after applying the digest schema migration.");
            return;
        }

        await Task.Delay(settings.InitialDelay, stoppingToken);

        using var timer = new PeriodicTimer(settings.Interval);
        do
        {
            await ProcessDigestBatchAsync(settings, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessDigestBatchAsync(NotificationDigestOptions settings, CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var now = timeProvider.GetUtcNow().UtcDateTime;

            // TODO: Once Notification entity has DigestFrequency, IsDigestDispatched, DigestScheduledFor:
            //   1. Query WHERE DigestFrequency IS NOT NULL AND IsDigestDispatched=false AND DigestScheduledFor <= now
            //   2. Group by (UserId, Channel, Frequency)
            //   3. Render digest template per group
            //   4. Dispatch via IEmailSender / InApp notification
            //   5. Mark items as dispatched (IsDigestDispatched=true, DigestBatchId=newGuid)

            logger.LogDebug(
                "Notification digest service ran at {UtcNow} — no-op until schema migration applied.",
                now);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Notification digest service encountered an error.");
        }
    }
}
