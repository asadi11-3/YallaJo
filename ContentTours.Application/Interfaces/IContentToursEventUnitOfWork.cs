namespace ContentTours.Application.Interfaces;

public interface IContentToursEventUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
