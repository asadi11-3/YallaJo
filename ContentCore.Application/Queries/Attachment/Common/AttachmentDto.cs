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
    Guid UploadedByUserId);
