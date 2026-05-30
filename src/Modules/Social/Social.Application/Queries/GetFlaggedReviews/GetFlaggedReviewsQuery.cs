using Social.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.GetFlaggedReviews;

public sealed record GetFlaggedReviewsQuery(
    Guid? AfterCursor,
    int PageSize = 20
) : IQuery<ReviewPageDto>, ICacheableQuery
{
    public string CacheKey => $"reviews:flagged:{AfterCursor}:{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["reviews:flagged"];
}
