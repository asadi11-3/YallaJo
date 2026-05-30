namespace Messaging.Application.Interfaces;

public interface IMessagingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
