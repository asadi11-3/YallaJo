using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Attachment.Common;
using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Attachment.GetEntityAttachments;

public sealed record GetEntityAttachmentsQuery(EntityType EntityType, Guid EntityId)
    : IQuery<IReadOnlyList<AttachmentDto>>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.EntityAttachments(EntityType.ToString(), EntityId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
    public IReadOnlyList<string> Tags => ["attachments", $"attachments:{EntityType}:{EntityId}"];
}
