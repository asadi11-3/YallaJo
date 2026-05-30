using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourGuides.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.TourGuides.GetByUserId;

/// <summary>
/// Resolves a tour guide profile by the authenticated user's identity.
/// Used by GET /api/v1/guides/me — the caller's <c>currentUser.UserId</c> maps to
/// <see cref="ContentTours.Domain.Entities.TourGuide.UserId"/>, NOT to the aggregate Id.
/// (Earlier the endpoint mistakenly sent <c>GetTourGuideByIdQuery</c>, which filters
/// by aggregate Id and therefore always returned NotFound — see F15.)
/// </summary>
public sealed record GetTourGuideByUserIdQuery(Guid UserId)
    : IQuery<TourGuideProfileDto>, ICacheableQuery
{
    public string CacheKey => TourGuideCacheKeys.ProfileByUser(UserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => [TourGuideCacheKeys.TagForProfileByUser(UserId)];
}

