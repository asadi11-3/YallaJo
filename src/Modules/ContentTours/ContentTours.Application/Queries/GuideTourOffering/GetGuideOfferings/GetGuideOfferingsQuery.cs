using ContentTours.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.GuideTourOffering.GetGuideOfferings;

public sealed record GuideOfferingDto(
    Guid Id, Guid TourId, Guid TourGuideId, string Status,
    bool OffersPrivateTour, decimal? PrivateTourPriceMultiplier, decimal? PrivateTourFlatPrice,
    bool IsProposer, DateTime? AssignedAt, DateTime CreatedAt);

public sealed record GetGuideOfferingsQuery(Guid TourId)
    : IQuery<IReadOnlyList<GuideOfferingDto>>, ICacheableQuery
{
    public string CacheKey => $"guide-offerings:{TourId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForTourOfferings(TourId)];
}
