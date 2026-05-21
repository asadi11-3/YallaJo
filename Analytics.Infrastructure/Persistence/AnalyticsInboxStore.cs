using Analytics.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace Analytics.Infrastructure.Persistence;

internal sealed class AnalyticsInboxStore(AnalyticsDbContext context) : IAnalyticsInboxStore
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
        => context.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

    public void MarkAsProcessed(Guid messageId)
        => context.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
}
