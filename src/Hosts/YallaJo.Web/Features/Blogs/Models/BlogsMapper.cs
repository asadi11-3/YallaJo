using YallaJo.Web.Features.Blogs.Helpers;
using YallaJo.Web.Services;

namespace YallaJo.Web.Features.Blogs.Models;

public static class BlogsMapper
{
   
    private const int BlogImageCount = 13;  
    private const int AvatarImageCount = 10; 

    // ── Cards ──────────────────────────────────────────────────────────────────
    public static BlogCardVm ToCardVm(BlogSummaryResponse r)
    {
        var image = ResolveBlogImage(r.Id);
        return new BlogCardVm
        {
            Id              = r.Id,
            Slug            = r.Slug,
            Title           = r.Title,
            Summary         = r.Summary,
            PublishedAt     = r.PublishedAt,
            ViewCount       = r.ViewCount,
            ReadTimeMinutes = r.ReadTimeMinutes,
            IsFeatured      = r.IsFeatured,
            ImageUrl        = image,
            ImageAlt        = r.Title,
            HasImage        = true,
        };
    }

    // ── Details ─────────────────────────────────────────────────────────────────
    public static BlogDetailsVm ToDetailsVm(
        BlogDetailResponse r,
        BlogAuthorVm? author,
        IReadOnlyList<BlogCommentVm> comments,
        IReadOnlyList<TagVm> popularTags)
    {
        var cover = ResolveBlogImage(r.Id);
        return new BlogDetailsVm
        {
            Id              = r.Id,
            Slug            = r.Slug,
            Title           = r.Title,
            // Sanitize creator-authored HTML at the mapper boundary so the raw
            // API content is never carried into (or rendered by) the view.
            ContentHtml     = ContentHtmlSanitizer.Sanitize(r.Content),
            Summary         = r.Summary,
            PublishedAt     = r.PublishedAt,
            ViewCount       = r.ViewCount,
            ReadTimeMinutes = r.ReadTimeMinutes,
            IsFeatured      = r.IsFeatured,
            TourCount       = r.TourCount,
            CoverImageUrl   = cover,
            CoverImageAlt   = r.Title,
            HasCoverImage   = true,
            Author          = author,
            Comments        = comments,
            PopularTags     = popularTags,
        };
    }

    public static BlogCommentVm ToCommentVm(BlogCommentResponse r) => new()
    {
        Id              = r.Id,
        ParentCommentId = r.ParentCommentId,
        Content         = r.IsContentRedacted ? "[deleted]" : r.Content,
        IsRedacted      = r.IsContentRedacted,
        LikeCount       = r.LikeCount,
        CreatedAt       = r.CreatedAt,
        UpdatedAt       = r.UpdatedAt,
    };

    public static BlogAuthorVm ToAuthorVm(CreatorProfileResponse r, IApiAssetUrlResolver resolver)
    {
        var resolved = resolver.Resolve(r.AvatarUrl);
        var hasImage = !string.IsNullOrWhiteSpace(resolved);
        return new BlogAuthorVm
        {
            DisplayName    = r.DisplayName,
            Slug           = r.Slug,
            Bio            = r.Bio,
            ArticleCount   = r.ArticleCount,
            FollowerCount  = r.FollowerCount,
            AuthorImageUrl = hasImage ? resolved! : ResolveAvatarPlaceholder(r.Id),
            AuthorImageAlt = r.DisplayName,
            HasAuthorImage = true,
        };
    }

    public static BlogNicheVm ToNicheVm(CreatorNicheResponse r) => new()
    {
        Id   = r.Id,
        Name = r.Name,
        Slug = r.Slug,
    };

    public static CategoryBadgeVm ToCategoryBadgeVm(CategoryResponse r) => new()
    {
        Id   = r.Id,
        Name = r.Name,
        Slug = r.Slug,
        Icon = r.Icon,
    };

    public static TagVm ToTagVm(TagResponse r) => new()
    {
        Id   = r.Id,
        Name = r.Name,
        Slug = r.Slug,
    };

    // ── Image resolution (deterministic placeholders) ───────────────────────────
    // Blog DTOs expose no image, and the attachment read endpoint is not publicly
    // accessible to anonymous visitors, so we render a stable theme placeholder
    // keyed by the blog id. See known API gap in the implementation report.
    public static string ResolveBlogImage(Guid id)
    {
        var index = (PositiveHash(id) % BlogImageCount) + 1;
        return $"/assets/images/blog/{index:00}.jpg";
    }

    private static string ResolveAvatarPlaceholder(Guid id)
    {
        var index = (PositiveHash(id) % AvatarImageCount) + 1;
        return $"/assets/images/avatar/{index:00}.jpg";
    }

    private static int PositiveHash(Guid id)
        => Math.Abs(id.GetHashCode()) is var h && h == int.MinValue ? 0 : Math.Abs(id.GetHashCode());
}
