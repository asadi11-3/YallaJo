using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class SlotCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<SlotCleanupOptions> options,
    ILogger<SlotCleanupService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly SlotCleanupOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SlotCleanupService disabled");
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
                logger.LogError(ex, "SlotCleanupService tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var slotRepo = scope.ServiceProvider.GetRequiredService<IAvailabilitySlotRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IBookingUnitOfWork>();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var cutoffDate = DateOnly.FromDateTime(nowUtc.AddDays(-_options.RetentionDays));

        var pastSlots = await slotRepo.GetInactivePastSlotsAsync(cutoffDate, _options.BatchSize, ct);

        if (pastSlots.Count == 0) return;

        foreach (var slot in pastSlots)
        {
            slotRepo.Remove(slot);
        }

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Cleaned up {Count} inactive past availability slots older than {Cutoff}",
            pastSlots.Count, cutoffDate);
    }
}

public sealed class SlotCleanupOptions
{
    public const string SectionName = "Booking:BackgroundServices:SlotCleanup";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromHours(24);
    public int RetentionDays { get; set; } = 30;
    public int BatchSize { get; set; } = 500;
}
