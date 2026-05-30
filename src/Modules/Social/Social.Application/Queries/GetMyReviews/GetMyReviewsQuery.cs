using Social.Application.Caching;
using Social.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.GetMyReviews;

public sealed record GetMyReviewsQuery(
    Guid UserId,
    Guid? AfterCursor,
    int PageSize = 20
) : IQuery<ReviewPageDto>, ICacheableQuery
{
    public string CacheKey => $"reviews:mine:{UserId}:{AfterCursor}:{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [SocialCacheKeys.UserReviewsTag(UserId)];
}
