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

    public const string FeaturedBlogsTag = "blogs:featured";

    public const string SitemapRenderedTag = "sitemap:rendered";

    public static string BlogTag(Guid id) => $"blog:{id}";

    public static string BlogSlugTag(string slug) =>
        $"blog:slug:{(slug ?? string.Empty).Trim().ToLowerInvariant()}";

    public static string BlogCommentsTag(Guid blogId) => $"blog:{blogId}:comments";

    public static string BlogToursTag(Guid blogId) => $"blog:{blogId}:tours";

    public static string BlogPlaceTag(Guid placeId) => $"blog:place:{placeId}";

    // ── Creator Cache Keys ───────────────────────────────────────────────
    public static string CreatorProfile(Guid profileId) =>
        $"cb:creator:profile:{profileId}";

    public static string CreatorProfileBySlug(string slug) =>
        $"cb:creator:profile:slug:{slug}";

    public static string CreatorProfileByUserId(Guid userId) =>
        $"cb:creator:profile:user:{userId}";

    public static string CreatorApplicationList(string filterHash, int page, int size) =>
        $"cb:creator:applications:{filterHash}:{page}:{size}";

    public static string CreatorApplication(Guid applicationId) =>
        $"cb:creator:application:{applicationId}";

    public static string CreatorFollowerList(Guid profileId, int page, int size) =>
        $"cb:creator:{profileId}:followers:{page}:{size}";

    public static string CreatorNicheList() =>
        "cb:creator:niches";

    public static string CreatorArticleList(Guid profileId, string filterHash, int page, int size) =>
        $"cb:creator:{profileId}:articles:{filterHash}:{page}:{size}";

    // ── Creator Cache Tags ───────────────────────────────────────────────
    public const string CreatorApplicationsListTag = "creator:applications:list";
    public const string CreatorNichesTag = "creator:niches";

    public static string CreatorProfileTag(Guid profileId) => $"creator:profile:{profileId}";
    public static string CreatorProfileSlugTag(string slug) =>
        $"creator:profile:slug:{(slug ?? string.Empty).Trim().ToLowerInvariant()}";
    public static string CreatorProfileUserTag(Guid userId) => $"creator:profile:user:{userId}";
    public static string CreatorApplicationTag(Guid applicationId) => $"creator:application:{applicationId}";
    public static string CreatorFollowersTag(Guid profileId) => $"creator:{profileId}:followers";
    public static string CreatorArticlesTag(Guid profileId) => $"creator:{profileId}:articles";

    // ── Creator Post Cache Keys (Wave-8) ────────────────────────────────
    public static string CreatorPost(string slug) => $"creators:posts:{slug}";
    public static string CreatorPostTag(string slug) => $"creators:posts:{slug}:tag";
    public const string CreatorPostsListTag = "creators:posts:list:tag";
    public const string CreatorPostsFeaturedTag = "creators:posts:featured:tag";
    public static string MyCreatorPosts(Guid profileId) => $"creators:my-posts:{profileId}";
    public static string MyCreatorPostsTag(Guid profileId) => $"creators:my-posts:{profileId}:tag";
    public const string AdminPostsQueueTag = "admin:posts:queue:tag";
}
