using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class SlotLockCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<SlotLockCleanupOptions> options,
    ILogger<SlotLockCleanupService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly SlotLockCleanupOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SlotLockCleanupService disabled");
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
                logger.LogError(ex, "SlotLockCleanupService tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var slotLockRepo = scope.ServiceProvider.GetRequiredService<ISlotLockRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IBookingUnitOfWork>();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var expiredLocks = await slotLockRepo.GetExpiredActiveAsync(nowUtc, _options.BatchSize, ct);
        foreach (var slotLock in expiredLocks)
        {
            slotLock.Release();
        }

        if (expiredLocks.Count > 0)
        {
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Released {Count} expired SlotLocks", expiredLocks.Count);
        }
    }
}

public sealed class SlotLockCleanupOptions
{
    public const string SectionName = "Booking:BackgroundServices:SlotLockCleanup";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(2);
    public int BatchSize { get; set; } = 200;
}
