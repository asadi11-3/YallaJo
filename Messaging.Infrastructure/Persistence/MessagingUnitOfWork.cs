using Messaging.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Messaging.Infrastructure.Persistence;

internal sealed class MessagingUnitOfWork(IUnitOfWork<MessagingDbContext> inner) : IMessagingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);
}
