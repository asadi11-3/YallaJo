using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Creator.Posts.ListPosts;

public sealed record ListCreatorPostsQuery(
    Guid? CreatorProfileId,
    CreatorPostType? TypeFilter,
    Guid? NicheId,
    string? Search,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResult<CreatorPostSummaryDto>>, ICacheableQuery
{
    public string CacheKey
    {
        get
        {
            var filter = $"creator:{CreatorProfileId}" +
                         $":type:{TypeFilter}" +
                         $":niche:{NicheId}" +
                         $":search:{NormalizeSearch(Search)}";
            return $"creators:posts:list:{filter}:{Page}:{PageSize}";
        }
    }

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(3);

    public IReadOnlyList<string> Tags
    {
        get
        {
            var tags = new List<string> { ContentBlogsCacheKeys.CreatorPostsListTag };
            if (CreatorProfileId.HasValue)
                tags.Add(ContentBlogsCacheKeys.MyCreatorPostsTag(CreatorProfileId.Value));
            return tags;
        }
    }

    private static string NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim().ToLowerInvariant();
}
