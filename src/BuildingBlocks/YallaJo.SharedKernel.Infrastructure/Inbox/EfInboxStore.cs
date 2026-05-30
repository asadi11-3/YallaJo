using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Application.Abstractions.Data;

namespace YallaJo.SharedKernel.Infrastructure.Inbox
{
    /// <summary>
    /// EF Core implementation of IInboxStore typed to a specific module DbContext.
    /// Each consuming module registers this against its own DbContext:
    ///   services.AddScoped&lt;IInboxStore, EfInboxStore&lt;AuthDbContext&gt;&gt;();
    ///
    /// HasBeenProcessedAsync: hits the DB to check for existing InboxMessage.
    /// MarkAsProcessed: adds to the EF change tracker only; persistence
    ///                  happens when the enclosing unit of work calls SaveChangesAsync.
    /// </summary>
    public sealed class EfInboxStore<TContext>(TContext dbContext) : IInboxStore
        where TContext : DbContext
    {
        public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
            => dbContext.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

        public void MarkAsProcessed(Guid messageId)
            => dbContext.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
    }
}
