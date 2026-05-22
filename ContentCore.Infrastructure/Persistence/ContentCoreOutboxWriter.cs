using ContentCore.Application.Interfaces;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentCore.Infrastructure.Persistence;

internal sealed class ContentCoreOutboxWriter(ContentCoreDbContext dbContext) : IContentCoreOutboxWriter
{
    public void Enqueue(IIntegrationEvent integrationEvent)
        => dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
}
