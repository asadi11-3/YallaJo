using ContentCore.Application.Caching;
using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Attachment.GetEntityAttachments;

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

public sealed record GetEntityAttachmentsQuery(EntityType EntityType, Guid EntityId)
    : IQuery<IReadOnlyList<AttachmentDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.EntityAttachments(EntityType.ToString(), EntityId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
    public IReadOnlyList<string> Tags => ["attachments", $"attachments:{EntityType}:{EntityId}"];
}
