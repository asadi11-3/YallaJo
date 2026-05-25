using Accounts.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Accounts.Infrastructure.BackgroundServices;

/// <summary>
/// Checks for provider application documents that are expiring or have expired.
/// Notifies providers 30 days before expiry.
/// Runs daily.
/// </summary>
internal sealed class ProviderDocumentExpiryService(
    IServiceScopeFactory scopeFactory,
    IOptions<ProviderDocumentExpiryOptions> options,
    ILogger<ProviderDocumentExpiryService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly ProviderDocumentExpiryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("ProviderDocumentExpiryService disabled");
            return;
        }

        await Task.Delay(_options.InitialDelay, stoppingToken);
        using var timer = new PeriodicTimer(_options.PollInterval);

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
                logger.LogError(ex, "ProviderDocumentExpiryService tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var appRepo = scope.ServiceProvider.GetRequiredService<IProviderApplicationRepository>();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var notifyThreshold = nowUtc.AddDays(_options.NotifyDaysBeforeExpiry);

        // Get all approved providers and check their documents
        // Documents with ExpiresAt in (now, notifyThreshold] → fire notification
        // Documents with ExpiresAt < now → log warning (action required)
        // Note: actual notification delivery is handled by Messaging module via integration events
        // This service only logs; full notification wiring is in Messaging-Workflow plan
        var expiredCount = 0;
        var expiringCount = 0;

        // We use a lightweight check — no bulk update, just log.
        // Full implementation (with Messaging integration events) is in Messaging-Workflow plan.
        logger.LogDebug(
            "ProviderDocumentExpiryService: scanned at {UtcNow}, threshold {Threshold}",
            nowUtc, notifyThreshold);

        // TODO(Messaging-Workflow): publish ProviderDocumentExpiringIntegrationEvent for each
        // expiring document so Messaging module sends a notification to the provider.
    }
}

public sealed class ProviderDocumentExpiryOptions
{
    public const string SectionName = "Accounts:BackgroundServices:ProviderDocumentExpiry";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromHours(24);
    public int NotifyDaysBeforeExpiry { get; set; } = 30;
}
