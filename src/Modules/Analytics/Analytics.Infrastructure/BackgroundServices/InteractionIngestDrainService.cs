using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Models;
using Analytics.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Analytics.Infrastructure.BackgroundServices;

internal sealed class InteractionIngestDrainService(
    IInteractionIngestQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<InteractionIngestDrainService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<InteractionEnvelope>(100);
        await foreach (var envelope in queue.ReadAllAsync(stoppingToken))
        {
            batch.Add(envelope);
            if (batch.Count < 100) continue;
            await FlushAsync(batch, stoppingToken);
            batch.Clear();
        }
        if (batch.Count > 0) await FlushAsync(batch, stoppingToken);
    }

    private async Task FlushAsync(IReadOnlyList<InteractionEnvelope> batch, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserInteractionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IAnalyticsUnitOfWork>();
        var interactions = batch.Select(e => UserInteraction.Record(e.UserId, e.SessionId, e.EntityType, e.EntityId, e.InteractionType, e.OccurredAt, e.ClientIpHash, e.UserAgent)).ToList();
        await repo.AddBatchAsync(interactions, ct);
        await uow.SaveChangesAsync(ct);
        logger.LogDebug("Flushed {Count} analytics interactions", interactions.Count);
    }
}
