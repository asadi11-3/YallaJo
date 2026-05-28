using Social.Application.Caching;
using Social.Application.Queries.Dtos;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.GetPublicReviews;

public sealed record GetPublicReviewsQuery(
    ReviewTargetType EntityType,
    Guid EntityId,
    int Page = 1,
    int PageSize = 20) : IQuery<PublicReviewPageDto>, ICacheableQuery
{
    public string CacheKey => SocialCacheKeys.PublicReviews(EntityType, EntityId, Math.Max(Page, 1), Math.Clamp(PageSize, 1, 50));
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [SocialCacheKeys.ReviewsTag(EntityType, EntityId)];
}
