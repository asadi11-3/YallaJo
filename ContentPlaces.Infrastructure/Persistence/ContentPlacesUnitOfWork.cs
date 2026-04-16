using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ContentPlaces.Infrastructure.Persistence;

internal sealed class ContentPlacesUnitOfWork(ContentPlacesDbContext context) : IContentPlacesUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ContentPlacesConcurrencyException(
                "A concurrency conflict occurred. Please refresh and try again.", ex);
        }
    }
}
