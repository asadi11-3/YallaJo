using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentPlaces.Infrastructure.Repositories;

internal sealed class ServiceItemRepository(ContentPlacesDbContext context)
    : EfRepository<ServiceItem, Guid>(context), IServiceItemRepository
{
}
