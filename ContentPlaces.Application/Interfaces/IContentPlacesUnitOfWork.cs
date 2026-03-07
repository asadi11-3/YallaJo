namespace ContentPlaces.Application.Interfaces;

public interface IContentPlacesUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
