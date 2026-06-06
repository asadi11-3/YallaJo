namespace YallaJo.Web.Areas.Creator.Models.Articles.Images;

/// <summary>
/// Pure mapping + client-side validation for article images. Dependency-free so the
/// ordering and file-validation rules can be unit-tested without a host. Limits mirror
/// the backend (ContentCore): images jpg/jpeg/png/gif/webp (no SVG), ≤ 10 MB each,
/// ≤ 20 per Blog.
/// </summary>
public static class ArticleImagesMapper
{
    public const int MaxImagesPerBlog = 20;
    public const long MaxImageBytes = 10L * 1024 * 1024; // 10 MB

    public static readonly IReadOnlyCollection<string> AllowedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public static readonly IReadOnlyCollection<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/gif", "image/webp" };

    public static ArticleImagesVm ToVm(Guid blogId, IEnumerable<AttachmentItemResponse>? items)
    {
        var rows = (items ?? [])
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.UploadedAt)
            .Select(i => new ArticleImageRowVm
            {
                Id               = i.Id,
                Url              = i.Url,
                ThumbnailUrl     = i.ThumbnailUrl,
                OriginalFileName = i.OriginalFileName,
                SortOrder        = i.SortOrder,
                IsPrimary        = i.IsPrimary,
            })
            .ToList();

        return new ArticleImagesVm
        {
            BlogId    = blogId,
            Images    = rows,
            MaxImages = MaxImagesPerBlog,
        };
    }

    /// <summary>
    /// Validates a single upload (extension, content type, size, non-empty). Returns null
    /// when valid, otherwise a user-facing error message. The server remains the source of
    /// truth; this is fail-fast UX.
    /// </summary>
    public static string? ValidateImage(string fileName, string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0)
            return $"\"{fileName}\" is empty.";

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return $"\"{fileName}\" has an unsupported type. Allowed: JPG, PNG, GIF, WEBP.";

        if (!string.IsNullOrWhiteSpace(contentType) && !AllowedContentTypes.Contains(contentType))
            return $"\"{fileName}\" has an unsupported content type. Allowed: JPG, PNG, GIF, WEBP.";

        if (sizeBytes > MaxImageBytes)
            return $"\"{fileName}\" exceeds the 10 MB image size limit.";

        return null;
    }
}
