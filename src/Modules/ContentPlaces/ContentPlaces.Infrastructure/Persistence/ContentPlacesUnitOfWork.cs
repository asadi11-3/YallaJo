using ContentPlaces.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentPlaces.Infrastructure.Persistence;

internal sealed class ContentPlacesUnitOfWork(IUnitOfWork<ContentPlacesDbContext> inner) : IContentPlacesUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await inner.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new DbUpdateConcurrencyException(
                "A concurrency conflict occurred. Please refresh and try again.", ex);
        }
    }
}
