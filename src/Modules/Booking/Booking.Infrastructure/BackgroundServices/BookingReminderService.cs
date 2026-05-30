using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class BookingReminderService(
    IServiceScopeFactory scopeFactory,
    IOptions<BookingReminderOptions> options,
    ILogger<BookingReminderService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly BookingReminderOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("BookingReminderService disabled");
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
                logger.LogError(ex, "BookingReminderService tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var bookingRepo = scope.ServiceProvider.GetRequiredService<ITourBookingRepository>();
        var outbox = scope.ServiceProvider.GetRequiredService<IBookingOutboxWriter>();
        var uow = scope.ServiceProvider.GetRequiredService<IBookingUnitOfWork>();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Reminder window: bookings starting between now and now + ReminderHours
        var windowStart = nowUtc;
        var windowEnd = nowUtc.AddHours(_options.ReminderHoursBeforeStart);

        var upcomingBookings = await bookingRepo.GetConfirmedWithUpcomingSlotsAsync(
            windowStart, windowEnd, _options.BatchSize, ct);

        if (upcomingBookings.Count == 0) return;

        foreach (var booking in upcomingBookings)
        {
            // Publish reminder event for Messaging module to handle
            await outbox.WriteAsync(new Booking.Contracts.IntegrationEvents.BookingReminderIntegrationEvent(
                booking.Id,
                booking.UserId,
                booking.TourId,
                booking.AvailabilitySlotId,
                nowUtc), ct);
        }

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Sent {Count} booking reminders", upcomingBookings.Count);
    }
}

public sealed class BookingReminderOptions
{
    public const string SectionName = "Booking:BackgroundServices:BookingReminder";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromHours(1);
    public int ReminderHoursBeforeStart { get; set; } = 24;
    public int BatchSize { get; set; } = 200;
}
