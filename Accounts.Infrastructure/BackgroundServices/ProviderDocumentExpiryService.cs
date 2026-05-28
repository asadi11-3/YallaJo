using Accounts.Contracts.IntegrationEvents;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Accounts.Infrastructure.BackgroundServices;

/// <summary>
/// Checks for provider application documents that are expiring or have expired.
/// Notifies providers 30 days before expiry via integration events.
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
        var outboxWriter = scope.ServiceProvider.GetRequiredService<IAccountsOutboxWriter>();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var notifyThreshold = nowUtc.AddDays(_options.NotifyDaysBeforeExpiry);

        var applications = await appRepo.GetApprovedWithExpiringDocumentsAsync(notifyThreshold, ct);

        var expiredCount = 0;
        var expiringCount = 0;

        foreach (var application in applications)
        {
            foreach (var doc in application.Documents.Where(d => d.ExpiresAt.HasValue))
            {
                if (doc.ExpiresAt!.Value < nowUtc)
                {
                    // Already expired — log warning
                    expiredCount++;
                    logger.LogWarning(
                        "Provider {UserId} document {DocType} ({FileName}) expired on {ExpiresAt}",
                        application.UserId, doc.DocumentType, doc.FileName, doc.ExpiresAt.Value);
                }
                else if (doc.ExpiresAt.Value <= notifyThreshold)
                {
                    // Expiring soon — publish integration event for Messaging module
                    expiringCount++;
                    var daysUntilExpiry = (int)(doc.ExpiresAt.Value - nowUtc).TotalDays;

                    await outboxWriter.WriteAsync(new ProviderDocumentExpiringIntegrationEvent(
                        ApplicationId: application.Id,
                        UserId: application.UserId,
                        DocumentType: doc.DocumentType.ToString(),
                        DocumentFileName: doc.FileName,
                        ExpiresAt: doc.ExpiresAt.Value,
                        DaysUntilExpiry: daysUntilExpiry), ct);
                }
            }
        }

        logger.LogInformation(
            "ProviderDocumentExpiryService: scanned {AppCount} applications, {ExpiredCount} expired docs, {ExpiringCount} expiring docs published",
            applications.Count, expiredCount, expiringCount);
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
