using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.AdminGetProfile;

/// <summary>Admin query — get full creator profile detail by ID.</summary>
public sealed record AdminGetCreatorProfileQuery(Guid ProfileId)
    : IQuery<CreatorProfileDto?>, ICacheableQuery
{
    public string CacheKey => ContentBlogsCacheKeys.CreatorProfile(ProfileId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [ContentBlogsCacheKeys.CreatorProfileTag(ProfileId)];
}
