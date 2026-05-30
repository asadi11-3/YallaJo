using Finance.Domain.Repositories;
using Finance.Infrastructure.Persistence;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Finance.Infrastructure.Repositories;

internal sealed class FinanceOutboxWriter(FinanceDbContext context) : IFinanceOutboxWriter
{
    public async Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var message = OutboxMessage.Create(integrationEvent);
        await context.OutboxMessages.AddAsync(message, ct);
    }
}
