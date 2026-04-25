using Messaging.Application.Interfaces;

namespace Messaging.Infrastructure.Persistence;

internal sealed class MessagingUnitOfWork(MessagingDbContext context) : IMessagingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
