using Social.Domain.Enums;

namespace Social.Application.Caching;

public static class SocialCacheKeys
{
    public static string ReviewsTag(ReviewTargetType entityType, Guid entityId) => $"reviews:entity:{entityType}:{entityId}";
    public static string ReviewTag(Guid reviewId) => $"review:{reviewId}";
    public static string UserReviewsTag(Guid userId) => $"reviews:user:{userId}";
    public static string FavoritesTag(Guid userId) => $"favorites:user:{userId}";
    public static string ReportsTag => "reports";

    public static string PublicReviews(ReviewTargetType entityType, Guid entityId, int page, int pageSize)
        => $"reviews:public:{entityType}:{entityId}:{page}:{pageSize}";

    public static string RatingSummary(ReviewTargetType entityType, Guid entityId)
        => $"reviews:ratings:{entityType}:{entityId}";
}
