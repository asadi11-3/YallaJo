using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentPlaces.Infrastructure.Repositories;

internal sealed class BusinessAmenityRepository(ContentPlacesDbContext context)
    : EfEntityRepository<BusinessAmenity, Guid>(context), IBusinessAmenityRepository
{
}
