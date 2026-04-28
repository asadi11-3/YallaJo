using ContentTours.Application.Interfaces;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.Persistence;

/// <summary>
/// Implements <see cref="IContentToursOutboxWriter"/> by staging outbox messages on
/// <see cref="ContentToursDbContext"/>. Changes are NOT saved here — they commit
/// atomically when <see cref="ContentToursUnitOfWork.SaveChangesAsync"/> runs.
/// </summary>
internal sealed class ContentToursOutboxWriter(ContentToursDbContext dbContext)
    : IContentToursOutboxWriter
{
    public void Enqueue(IIntegrationEvent integrationEvent)
        => dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
}
