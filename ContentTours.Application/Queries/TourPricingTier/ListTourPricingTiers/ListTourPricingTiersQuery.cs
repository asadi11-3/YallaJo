using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourPricingTier.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourPricingTier.ListTourPricingTiers;

/// <summary>
/// Lists pricing tiers for a tour with visibility-partitioned caching.
///
/// Visibility (P1 #2 — cache-key fix): the endpoint MUST populate
/// <see cref="CallerUserId"/> and <see cref="IsAdmin"/> from the authenticated
/// principal BEFORE dispatching this query. The cache layer uses these values
/// — not <c>ICurrentUser</c> inside the handler — to build <see cref="CacheKey"/>,
/// so anonymous and admin responses cannot collide on the same cache slot.
///
/// Effective <c>activeOnly</c> rule (preserved from prior behaviour):
///   - non-elevated caller (anonymous, or authenticated non-owner non-admin)
///     → effective activeOnly = true regardless of <see cref="ActiveOnly"/>.
///   - admin OR owner of the parent tour → respect <see cref="ActiveOnly"/>.
///
/// The handler enforces this; the cache key partitions on caller identity so
/// a future admin call with <c>ActiveOnly=false</c> never poisons the anonymous
/// slot.
/// </summary>
public sealed record ListTourPricingTiersQuery(
    Guid TourId,
    bool ActiveOnly,
    string? LanguageCode,
    /// <summary>The authenticated user's id, or null for anonymous callers.</summary>
    Guid? CallerUserId,
    /// <summary>True when the caller carries the Admin role.</summary>
    bool IsAdmin)
    : IQuery<IReadOnlyList<TourPricingTierDto>>, ICacheableQuery
{
    public string CacheKey => TourPricingTierCacheKeys.List(
        TourId, ActiveOnly, LanguageCode, CallerUserId, IsAdmin);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags =>
        [TourPricingTierCacheKeys.TagForTour(TourId), TourCacheKeys.TagForTour(TourId)];
}
