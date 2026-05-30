using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using ContentTours.Contracts.Tours;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.BackgroundServices;

/// <summary>
/// Daily background service that generates AvailabilitySlots for the rolling 60-day
/// window based on active GuideSchedules from ContentTours.
/// </summary>
internal sealed class SlotGenerationService(
    IServiceScopeFactory scopeFactory,
    IOptions<SlotGenerationOptions> options,
    ILogger<SlotGenerationService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly SlotGenerationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SlotGenerationService disabled.");
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
                logger.LogError(ex, "SlotGenerationService tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var scheduleReader = scope.ServiceProvider.GetRequiredService<IGuideScheduleReader>();
        var slotRepository = scope.ServiceProvider.GetRequiredService<IAvailabilitySlotRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<Booking.Application.Interfaces.IBookingUnitOfWork>();

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var windowEnd = today.AddDays(_options.WindowDays);

        var schedules = await scheduleReader.GetActiveSchedulesAsync(ct).ConfigureAwait(false);

        var totalCreated = 0;

        foreach (var schedule in schedules)
        {
            ct.ThrowIfCancellationRequested();

            var existingDates = await slotRepository.GetExistingSlotDatesAsync(
                schedule.TourGuideId, schedule.TourId, today, windowEnd, ct).ConfigureAwait(false);

            var endTime = schedule.EndTime ?? schedule.StartTime.AddHours(2);
            var dayOfWeek = (DayOfWeek)schedule.DayOfWeek;

            // Walk the window and generate slots for matching days-of-week
            var current = today;
            while (current <= windowEnd)
            {
                if (current.DayOfWeek == dayOfWeek && !existingDates.Contains(current))
                {
                    var slot = AvailabilitySlot.CreateForTour(
                        schedule.TourGuideId,
                        schedule.TourId,
                        current,
                        schedule.StartTime,
                        endTime,
                        maxCapacity: _options.DefaultMaxCapacity);

                    await slotRepository.AddAsync(slot, ct).ConfigureAwait(false);
                    totalCreated++;
                }

                current = current.AddDays(1);
            }
        }

        if (totalCreated > 0)
        {
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("SlotGenerationService created {Count} availability slots for {Window}-day window.", totalCreated, _options.WindowDays);
        }
    }
}

public sealed class SlotGenerationOptions
{
    public const string SectionName = "Booking:BackgroundServices:SlotGeneration";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromHours(24);
    public int WindowDays { get; set; } = 60;
    public int DefaultMaxCapacity { get; set; } = 10;
}
