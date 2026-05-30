using ContentTours.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace ContentTours.Infrastructure.Persistence;

internal sealed class ContentToursInboxStore(ContentToursDbContext context) : IContentToursInboxStore
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
        => context.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

    public void MarkAsProcessed(Guid messageId)
        => context.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
}
