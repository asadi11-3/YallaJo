using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class DocumentExpiryCheckService(
    IServiceScopeFactory scopeFactory,
    IOptions<DocumentExpiryCheckOptions> options,
    ILogger<DocumentExpiryCheckService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly DocumentExpiryCheckOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("DocumentExpiryCheckService disabled");
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
                logger.LogError(ex, "DocumentExpiryCheckService tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var documentRepo = scope.ServiceProvider.GetRequiredService<IProviderDocumentRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IBookingUnitOfWork>();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var expiringThresholdUtc = nowUtc.AddDays(_options.ExpiringSoonWindowDays);

        var expiredDocuments = await documentRepo.GetNewlyExpiredAsync(nowUtc, ct);
        foreach (var document in expiredDocuments)
        {
            document.MarkExpired(nowUtc);
        }

        var expiringDocuments = await documentRepo.GetExpiringSoonAsync(nowUtc, expiringThresholdUtc, ct);
        foreach (var document in expiringDocuments)
        {
            document.MarkExpiring(nowUtc);
        }

        var changedCount = expiredDocuments.Count + expiringDocuments.Count;
        if (changedCount > 0)
        {
            await uow.SaveChangesAsync(ct);
            logger.LogInformation(
                "Processed {ExpiredCount} expired and {ExpiringCount} expiring ProviderDocuments",
                expiredDocuments.Count,
                expiringDocuments.Count);
        }
    }
}

public sealed class DocumentExpiryCheckOptions
{
    public const string SectionName = "Booking:BackgroundServices:DocumentExpiryCheck";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromHours(24);
    public int ExpiringSoonWindowDays { get; set; } = 30;
}
