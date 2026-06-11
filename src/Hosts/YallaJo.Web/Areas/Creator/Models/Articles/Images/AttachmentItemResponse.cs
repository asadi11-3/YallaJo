namespace YallaJo.Web.Areas.Creator.Models.Articles.Images;

/// <summary>
/// Web-side projection of the ContentCore <c>AttachmentDto</c> returned by
/// GET /api/v1/content-core/attachments. The <see cref="IsPrimary"/> flag (Gap 2)
/// now lets the UI highlight the current primary image.
/// </summary>
public sealed class AttachmentItemResponse
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public string? OriginalFileName { get; init; }
    public string? MimeType { get; init; }
    public long? FileSize { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? DurationSeconds { get; init; }
    public int SortOrder { get; init; }
    public DateTime UploadedAt { get; init; }
    public Guid UploadedByUserId { get; init; }

    // ── Creator Backend Contract Polish (Gap 2) ─────────────────────────────
    /// <summary>Whether this attachment is the entity's current primary image.</summary>
    public bool IsPrimary { get; init; }
}

/// <summary>Response of the single-upload endpoint (POST /attachments).</summary>
public sealed class UploadAttachmentResponse
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public long FileSize { get; init; }
}

/// <summary>
/// Response of the bulk image-upload endpoint
/// (POST /api/v1/content-core/attachments/images, up to 20 files in one request — API7).
/// The server uploads each file and reports per-file failures in <see cref="Errors"/>
/// without aborting the whole batch.
/// </summary>
public sealed class BulkUploadImagesResponse
{
    public IReadOnlyList<Guid> UploadedAttachmentIds { get; init; } = [];
    public IReadOnlyList<string> Errors { get; init; } = [];
}
