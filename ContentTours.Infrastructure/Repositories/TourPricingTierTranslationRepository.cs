using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using ContentTours.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentTours.Infrastructure.Repositories;

internal sealed class TourPricingTierTranslationRepository(ContentToursDbContext context)
    : EfEntityRepository<TourPricingTierTranslation, Guid>(context), ITourPricingTierTranslationRepository
{
}
