using Messaging.Contracts.IntegrationEvents;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messaging.Infrastructure.BackgroundServices;

public sealed class SlaMonitoringOptions
{
    public const string SectionName = "Messaging:BackgroundServices:SlaMonitoring";

    public bool Enabled { get; init; } = true;
    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMinutes(3);
    public int BatchSize { get; init; } = 100;
}

/// <summary>
/// Periodically checks for support tickets that have breached their SLA deadline
/// and publishes <see cref="SupportSlaBreachedIntegrationEvent"/> for each.
/// </summary>
internal sealed class SlaMonitoringService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<SlaMonitoringOptions> options,
    ILogger<SlaMonitoringService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("SLA monitoring service is disabled.");
            return;
        }

        await Task.Delay(settings.InitialDelay, stoppingToken);

        using var timer = new PeriodicTimer(settings.Interval);
        do
        {
            await CheckSlaBreachesAsync(settings, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckSlaBreachesAsync(SlaMonitoringOptions settings, CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var ticketRepo = scope.ServiceProvider.GetRequiredService<ISupportTicketRepository>();
            var outboxWriter = scope.ServiceProvider.GetRequiredService<IMessagingOutboxWriter>();

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var overdueTickets = await ticketRepo.GetOverdueSlaTicketsAsync(now, settings.BatchSize, ct);

            if (overdueTickets.Count == 0) return;

            logger.LogInformation("SLA monitoring: found {Count} overdue tickets.", overdueTickets.Count);

            foreach (var ticket in overdueTickets)
            {
                var evt = new SupportSlaBreachedIntegrationEvent(
                    TicketId: ticket.Id,
                    CreatedByUserId: ticket.CreatedByUserId,
                    AssignedToUserId: ticket.AssignedToUserId,
                    Priority: ticket.Priority.ToString(),
                    SlaBreachAt: ticket.SlaBreachAt,
                    DetectedAt: now);

                await outboxWriter.WriteAsync(evt, ct);
            }

            logger.LogInformation("SLA monitoring: published {Count} SLA breach events.", overdueTickets.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "SLA monitoring service encountered an error.");
        }
    }
}
