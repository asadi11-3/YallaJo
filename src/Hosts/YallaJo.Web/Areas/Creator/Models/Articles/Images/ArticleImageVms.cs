namespace YallaJo.Web.Areas.Creator.Models.Articles.Images;

/// <summary>
/// The article-images section of the Article Editor. Populated on GET edit (once a
/// BlogId exists). No DTOs leak into the Razor view.
/// </summary>
public sealed class ArticleImagesVm
{
    /// <summary>Owning Blog article id (the editor's article).</summary>
    public Guid BlogId { get; init; }

    /// <summary>Existing images, ordered by SortOrder.</summary>
    public IReadOnlyList<ArticleImageRowVm> Images { get; init; } = [];

    /// <summary>Backend per-Blog image cap (AttachmentLimits.Blog = 20).</summary>
    public int MaxImages { get; init; } = 20;

    public bool HasImages => Images.Count > 0;

    public bool CanAddMore => Images.Count < MaxImages;

    public int RemainingSlots => Math.Max(0, MaxImages - Images.Count);
}

/// <summary>One image thumbnail row. No IsPrimary flag — the attachment DTO does not
/// expose which image is the current primary (CCD-5 gap B).</summary>
public sealed class ArticleImageRowVm
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public string? OriginalFileName { get; init; }
    public int SortOrder { get; init; }

    /// <summary>Best display URL: thumbnail if present, else full image.</summary>
    public string DisplayUrl => string.IsNullOrWhiteSpace(ThumbnailUrl) ? Url : ThumbnailUrl!;
}
