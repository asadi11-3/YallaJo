using ContentCore.Domain.Enums;

namespace ContentCore.Application.Queries.Attachment.Common;

public sealed record AttachmentDto(
    Guid Id,
    EntityType EntityType,
    Guid EntityId,
    AttachmentType Type,
    string Url,
    string? ThumbnailUrl,
    string? OriginalFileName,
    string? MimeType,
    long? FileSize,
    int? Width,
    int? Height,
    int? DurationSeconds,
    int SortOrder,
    DateTime UploadedAt,
    Guid UploadedByUserId,
    // ── Creator Backend Contract Polish (Gap 2) — appended, optional ─────────
    // Whether this attachment is the primary image for its entity, sourced from
    // the EntityImage join (EntityImage.IsPrimary). Defaults to false so existing
    // positional constructions (e.g. GetAttachmentById, Tours) remain compatible.
    bool IsPrimary = false);
