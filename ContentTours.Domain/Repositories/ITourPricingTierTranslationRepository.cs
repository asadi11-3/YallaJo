using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface ITourPricingTierTranslationRepository
    : IReadRepository<TourPricingTierTranslation, Guid>, IWriteRepository<TourPricingTierTranslation, Guid>
{
}
