namespace ContentSeo.Application.Interfaces;

public interface IContentSeoUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
