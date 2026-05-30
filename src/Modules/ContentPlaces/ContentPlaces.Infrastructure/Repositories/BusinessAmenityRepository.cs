using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentPlaces.Infrastructure.Repositories;

internal sealed class BusinessAmenityRepository(ContentPlacesDbContext context)
    : EfEntityRepository<BusinessAmenity, Guid>(context),
      IBusinessAmenityRepository
{
    public async Task<BusinessAmenity?> GetByIdWithBusinessAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await context.BusinessAmenities
            .Include(x => x.Business)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
