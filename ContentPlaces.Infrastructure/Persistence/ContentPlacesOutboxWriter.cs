using ContentPlaces.Application.Interfaces;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.Persistence;

/// <summary>
/// Implements <see cref="IContentPlacesOutboxWriter"/> by staging outbox messages on
/// <see cref="ContentPlacesDbContext"/>. Changes are NOT saved here — they commit
/// atomically when <see cref="ContentPlacesUnitOfWork.SaveChangesAsync"/> runs.
/// </summary>
internal sealed class ContentPlacesOutboxWriter(ContentPlacesDbContext dbContext)
    : IContentPlacesOutboxWriter
{
    public void Enqueue(IIntegrationEvent integrationEvent)
        => dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
}
