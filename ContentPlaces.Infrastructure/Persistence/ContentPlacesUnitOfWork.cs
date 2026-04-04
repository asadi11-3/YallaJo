using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace ContentPlaces.Infrastructure.Persistence;

internal sealed class ContentPlacesUnitOfWork(IUnitOfWork<ContentPlacesDbContext> unitOfWork)
    : IContentPlacesUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentPlaceConcurrencyException();
        }
    }
}
