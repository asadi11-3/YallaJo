
using Microsoft.EntityFrameworkCore;
using Security.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace Security.Infrastructure.Persistence;

internal sealed class SecurityInboxStore(SecurityDbContext context) : ISecurityInboxStore
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
        => context.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

    public void MarkAsProcessed(Guid messageId)
        => context.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
}
