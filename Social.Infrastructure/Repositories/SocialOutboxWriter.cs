using Social.Domain.Repositories;
using Social.Infrastructure.Persistence;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Social.Infrastructure.Repositories;

internal sealed class SocialOutboxWriter(SocialDbContext context) : ISocialOutboxWriter
{
    public async Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var message = OutboxMessage.Create(integrationEvent);
        await context.OutboxMessages.AddAsync(message, ct).ConfigureAwait(false);
    }
}
