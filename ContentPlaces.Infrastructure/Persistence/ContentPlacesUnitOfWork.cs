using ContentPlaces.Application.Interfaces;

namespace ContentPlaces.Infrastructure.Persistence;

internal sealed class ContentPlacesUnitOfWork(ContentPlacesDbContext context) : IContentPlacesUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
