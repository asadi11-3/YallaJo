using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace ContentSeo.Infrastructure.Persistence;

internal sealed class ContentSeoInboxStore(ContentSeoDbContext context) : IContentSeoInboxStore
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
        => context.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

    public void MarkAsProcessed(Guid messageId)
        => context.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
}
