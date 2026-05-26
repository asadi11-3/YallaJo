using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class BookingAutoExpireService(
    IServiceScopeFactory scopeFactory,
    IOptions<BookingAutoExpireOptions> options,
    ILogger<BookingAutoExpireService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly BookingAutoExpireOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("BookingAutoExpireService disabled");
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
                logger.LogError(ex, "BookingAutoExpireService tick failed");
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

        var expiredBookings = await bookingRepo.GetExpiredAwaitingPaymentAsync(nowUtc, _options.BatchSize, ct);
        foreach (var booking in expiredBookings)
        {
            booking.MoveToAwaitingPaymentExpired();
        }

        if (expiredBookings.Count > 0)
        {
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Expired {Count} AwaitingPayment bookings", expiredBookings.Count);
        }
    }
}

public sealed class BookingAutoExpireOptions
{
    public const string SectionName = "Booking:BackgroundServices:BookingAutoExpire";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(10);
    public int BatchSize { get; set; } = 100;
}
