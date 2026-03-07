namespace ContentTours.Application.Interfaces;

public interface IContentToursUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
