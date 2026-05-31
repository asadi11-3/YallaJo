using ContentTours.Application.Caching;
using ContentTours.Application.Queries.Tour.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.GetTourById;

// F45 2026-05-30: a Draft/non-Approved tour was invisible via GET /tours/{id}
// (404) even to its owner and to admins. Added RequestingUserId + IsPrivileged so
// the owner (Tour.CreatedByUserId == RequestingUserId) or an admin can read a tour
// regardless of status, while the public still only sees Approved tours. The
// elevated view is cached under a separate key (isElevated) so a draft can never
// leak into the public cache entry.
public sealed record GetTourByIdQuery(
    Guid Id,
    string? AcceptLanguage = null,
    Guid? RequestingUserId = null,
    bool IsPrivileged = false)
    : IQuery<TourDetailDto>, ICacheableQuery
{
    public string CacheKey =>
        ContentToursCacheKeys.Tour(Id, AcceptLanguage, isElevated: IsPrivileged || RequestingUserId is not null);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags =>
        [ContentToursCacheKeys.TagToursList, ContentToursCacheKeys.TagForTour(Id)];
}
