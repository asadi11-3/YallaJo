using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.AccessibilityFeature.GetAccessibilityFeatures;

public sealed record GetAccessibilityFeaturesQuery(Guid PlaceId)
    : IQuery<IReadOnlyList<AccessibilityFeatureDto>>, ICacheableQuery
{
    public string CacheKey => $"content_places:place_accessibility:{PlaceId}";

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
    [
        ContentPlacesCacheKeys.TagPlaces,
        ContentPlacesCacheKeys.TagForPlace(PlaceId),
    ];
}
