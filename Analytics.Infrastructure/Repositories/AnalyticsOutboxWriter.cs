using Analytics.Domain.Repositories;
using Analytics.Infrastructure.Persistence;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Analytics.Infrastructure.Repositories;

internal sealed class AnalyticsOutboxWriter(AnalyticsDbContext context) : IAnalyticsOutboxWriter
{
    public async Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var message = OutboxMessage.Create(integrationEvent);
        await context.OutboxMessages.AddAsync(message, ct);
    }
}
