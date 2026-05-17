using Messaging.Domain.Repositories;
using Messaging.Infrastructure.Persistence;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Messaging.Infrastructure.Repositories;

internal sealed class MessagingOutboxWriter(MessagingDbContext context) : IMessagingOutboxWriter
{
    public async Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var message = OutboxMessage.Create(integrationEvent);
        await context.OutboxMessages.AddAsync(message, ct);
    }
}
