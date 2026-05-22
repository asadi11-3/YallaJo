using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class ProviderAutoAcceptService(
    IServiceScopeFactory scopeFactory,
    IOptions<ProviderAutoAcceptOptions> options,
    ILogger<ProviderAutoAcceptService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly ProviderAutoAcceptOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("ProviderAutoAcceptService disabled");
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
                logger.LogError(ex, "ProviderAutoAcceptService tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var bookingRepo = scope.ServiceProvider.GetRequiredService<ITourBookingRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IBookingUnitOfWork>();
        var cutoffUtc = timeProvider.GetUtcNow().UtcDateTime.Subtract(_options.AutoAcceptAfter);

        var bookings = await bookingRepo.GetPendingConfirmationOlderThanAsync(cutoffUtc, _options.BatchSize, ct);
        foreach (var booking in bookings)
        {
            booking.Confirm(ConfirmationSource.AutoAccept);
        }

        if (bookings.Count > 0)
        {
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Auto-confirmed {Count} pending provider bookings", bookings.Count);
        }
    }
}

public sealed class ProviderAutoAcceptOptions
{
    public const string SectionName = "Booking:BackgroundServices:ProviderAutoAccept";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan AutoAcceptAfter { get; set; } = TimeSpan.FromHours(20);
    public int BatchSize { get; set; } = 100;
}
