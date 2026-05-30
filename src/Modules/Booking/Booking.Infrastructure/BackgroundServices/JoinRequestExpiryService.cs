using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.BackgroundServices;

internal sealed class JoinRequestExpiryService(
    IServiceScopeFactory scopeFactory,
    IOptions<JoinRequestExpiryOptions> options,
    ILogger<JoinRequestExpiryService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly JoinRequestExpiryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("JoinRequestExpiryService disabled");
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
                logger.LogError(ex, "JoinRequestExpiryService tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var joinRequestRepo = scope.ServiceProvider.GetRequiredService<IJoinRequestRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IBookingUnitOfWork>();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var expiredRequests = await joinRequestRepo.GetExpiredPendingAsync(nowUtc, ct);
        foreach (var request in expiredRequests)
        {
            var result = request.Expire(nowUtc);
            if (!result.IsSuccess)
            {
                logger.LogWarning("Failed to expire JoinRequest {Id}: {Error}", request.Id, result.Errors.FirstOrDefault()?.Message);
            }
        }

        if (expiredRequests.Count > 0)
        {
            await uow.SaveChangesAsync(ct);
            logger.LogInformation("Expired {Count} JoinRequests", expiredRequests.Count);
        }
    }
}

public sealed class JoinRequestExpiryOptions
{
    public const string SectionName = "Booking:BackgroundServices:JoinRequestExpiry";
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromHours(1);
    public int BatchSize { get; set; } = 200;
}
