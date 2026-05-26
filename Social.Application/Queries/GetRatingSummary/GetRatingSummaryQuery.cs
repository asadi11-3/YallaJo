using Social.Application.Caching;
using Social.Application.Queries.Dtos;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.GetRatingSummary;

public sealed record GetRatingSummaryQuery(ReviewTargetType EntityType, Guid EntityId)
    : IQuery<RatingSummaryDto>, ICacheableQuery
{
    public string CacheKey => SocialCacheKeys.RatingSummary(EntityType, EntityId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [SocialCacheKeys.ReviewsTag(EntityType, EntityId)];
}
