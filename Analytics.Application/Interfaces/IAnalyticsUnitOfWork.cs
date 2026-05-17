namespace Analytics.Application.Interfaces;

public interface IAnalyticsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
