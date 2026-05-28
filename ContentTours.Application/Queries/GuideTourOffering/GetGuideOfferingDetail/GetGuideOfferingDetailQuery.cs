using ContentTours.Application.Caching;
using ContentTours.Application.Queries.GuideTourOffering.GetGuidePricingTiers;
using ContentTours.Application.Queries.GuideTourOffering.GetGuideSchedules;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.GuideTourOffering.GetGuideOfferingDetail;

public sealed record GuideOfferingDetailDto(
    Guid Id, Guid TourId, Guid TourGuideId, string Status,
    bool OffersPrivateTour, decimal? PrivateTourPriceMultiplier, decimal? PrivateTourFlatPrice,
    bool IsProposer, DateTime? AssignedAt, DateTime CreatedAt,
    IReadOnlyList<GuideScheduleDto> Schedules,
    IReadOnlyList<GuidePricingTierDto> PricingTiers);

public sealed record GetGuideOfferingDetailQuery(Guid TourId, Guid TourGuideId)
    : IQuery<GuideOfferingDetailDto>, ICacheableQuery
{
    public string CacheKey => $"guide-offering-detail:{TourId}:{TourGuideId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForTourOfferings(TourId)];
}
