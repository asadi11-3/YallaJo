using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Attachment : BaseEntity
{
    private Attachment() { } // EF Core

    public EntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public AttachmentType Type { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? ThumbnailUrl { get; private set; }
    public string? OriginalFileName { get; private set; }
    public string? MimeType { get; private set; }
    public long? FileSize { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public int? DurationSeconds { get; private set; }
    public int SortOrder { get; private set; }
    public byte[]? Iv { get; private set; }
    public byte[]? Hmac { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Guid UploadedByUserId { get; private set; }
}
