using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.BackgroundServices;

/// <summary>
/// Automatically completes Confirmed bookings after the tour slot date/time has passed.
/// Runs every hour. Uses Guid.Empty as the system actor (automated completion).
/// </summary>
internal sealed class BookingAutoCompleteService(
    IServiceScopeFactory scopeFactory,
    IOptions<BookingAutoCompleteOptions> options,
    ILogger<BookingAutoCompleteService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private static readonly Guid SystemActorId = new("00000000-0000-0000-0000-000000000001");
    private readonly BookingAutoCompleteOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("BookingAutoCompleteService disabled");
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
                logger.LogError(ex, "BookingAutoCompleteService tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var bookingRepo = scope.ServiceProvider.GetRequiredService<ITourBookingRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IBookingUnitOfWork>();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var completableBookings = await bookingRepo.GetConfirmedWithPastSlotAsync(nowUtc, _options.BatchSize, ct);
        var completed = 0;
        foreach (var booking in completableBookings)
        {
            try
            {
                booking.Complete(SystemActorId);
                completed++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to auto-complete booking {Id}", booking.Id);
            }
        }

        if (completed > 0)
        {
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Auto-completed {Count} bookings", completed);
        }
    }
}

public sealed class BookingAutoCompleteOptions
{
    public const string SectionName = "Booking:BackgroundServices:BookingAutoComplete";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromHours(1);
    public int BatchSize { get; set; } = 100;
}
