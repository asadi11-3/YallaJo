using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class TourPricingTierRepository(ContentToursDbContext context)
    : EfEntityRepository<TourPricingTier, Guid>(context), ITourPricingTierRepository
{
}
