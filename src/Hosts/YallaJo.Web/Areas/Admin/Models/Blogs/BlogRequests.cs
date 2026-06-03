namespace YallaJo.Web.Areas.Admin.Models.Blogs;

/// <summary>Mirrors the API <c>CreateBlogRequest</c> for <c>POST /api/v1/blogs</c>.</summary>
public sealed record CreateBlogRequest(
    string Title,
    string Content,
    string SourceLanguageCode,
    string? Slug = null,
    string? Summary = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    Guid? PlaceId = null);

/// <summary>Mirrors the API <c>UpdateBlogRequest</c> for <c>PUT /api/v1/blogs/{id}</c>.</summary>
public sealed record UpdateBlogRequest(
    byte[] RowVersion,
    string Title,
    string Slug,
    string Content,
    string? Summary = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    Guid? PlaceId = null,
    int? ReadTimeMinutes = null);

/// <summary>Mirrors the API <c>BlogRowVersionRequest</c> (delete/restore/status actions).</summary>
public sealed record BlogRowVersionRequest(byte[] RowVersion);

/// <summary>Mirrors the API <c>FeatureBlogRequest</c> for <c>POST /api/v1/blogs/{id}/feature</c>.</summary>
public sealed record FeatureBlogRequest(byte[] RowVersion, DateTime? FeaturedUntil = null);

/// <summary>Mirrors the API <c>RejectBlogRequest</c> for <c>POST /api/v1/blogs/admin/{id}/reject</c>.</summary>
public sealed record RejectBlogRequest(byte[] RowVersion, string Reason);
