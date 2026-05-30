using Accounts.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace Accounts.Infrastructure.Persistence;

internal sealed class AccountsInboxStore(AccountsDbContext context) : IAccountsInboxStore
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
        => context.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

    public void MarkAsProcessed(Guid messageId)
        => context.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
}
