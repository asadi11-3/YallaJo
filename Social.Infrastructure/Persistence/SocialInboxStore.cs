using Microsoft.EntityFrameworkCore;
using Social.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace Social.Infrastructure.Persistence;

/// <summary>Module-scoped inbox store for Social integration event handlers.</summary>
internal sealed class SocialInboxStore(SocialDbContext context) : ISocialInboxStore
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
        => context.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

    public void MarkAsProcessed(Guid messageId)
        => context.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
}
