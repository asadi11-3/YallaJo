using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.GetBusinessAccessibilityFeatures;

public sealed record GetBusinessAccessibilityFeaturesQuery(Guid BusinessId)
    : IQuery<IReadOnlyList<AccessibilityFeatureDto>>, ICacheableQuery
{
    public string CacheKey => $"content_places:business_accessibility:{BusinessId}";

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
    [
        ContentPlacesCacheKeys.TagBusinesses,
        ContentPlacesCacheKeys.TagForBusiness(BusinessId),
    ];
}
