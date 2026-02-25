using Accounts.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace Accounts.Infrastructure.Persistence
{
    internal sealed class AccountsInboxStore(AccountsDbContext context) : IAccountsInboxStore
    {
        public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
            => context.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

        public void MarkAsProcessed(Guid messageId)
            => context.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
    }
}
