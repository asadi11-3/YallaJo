namespace ContentBlogs.Application.Caching;

public static class ContentBlogsCacheKeys
{
   

    public static string BlogList(string stableFilterHash, int page, int size) =>
        $"cb:blogs:{stableFilterHash}:{page}:{size}";

    public static string Blog(Guid id, string languageCode) =>
        $"cb:blog:{id}:lang:{languageCode}";

    public static string BlogBySlug(string slug, string languageCode) =>
        $"cb:blog:slug:{slug}:lang:{languageCode}";

    public static string BlogComments(Guid blogId, int page, int size) =>
        $"cb:blog:{blogId}:comments:{page}:{size}";

    public static string BlogTours(Guid blogId) =>
        $"cb:blog:{blogId}:tours";


    public const string BlogsListTag = "blogs:list";

    public const string SitemapRenderedTag = "sitemap:rendered";

    public static string BlogTag(Guid id) => $"blog:{id}";

    public static string BlogSlugTag(string slug) =>
        $"blog:slug:{(slug ?? string.Empty).Trim().ToLowerInvariant()}";

    public static string BlogCommentsTag(Guid blogId) => $"blog:{blogId}:comments";

    public static string BlogToursTag(Guid blogId) => $"blog:{blogId}:tours";

    public static string BlogPlaceTag(Guid placeId) => $"blog:place:{placeId}";
}
