using Finance.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace Finance.Infrastructure.Persistence;

/// <summary>
/// EF Core inbox store typed to <see cref="FinanceDbContext"/>.
/// Used by Finance integration event handlers to guarantee at-most-once processing
/// of cross-module messages (booking.tour-booking.created.v1, etc.).
/// </summary>
internal sealed class FinanceInboxStore(FinanceDbContext context) : IFinanceInboxStore
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
        => context.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

    public void MarkAsProcessed(Guid messageId)
        => context.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
}
