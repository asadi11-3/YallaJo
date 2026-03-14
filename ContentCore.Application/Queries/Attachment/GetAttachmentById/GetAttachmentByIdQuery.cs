using ContentCore.Application.Caching;
using ContentCore.Application.Queries.Attachment.GetEntityAttachments;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Attachment.GetAttachmentById;

public sealed record GetAttachmentByIdQuery(Guid AttachmentId)
    : IQuery<AttachmentDto>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.Attachment(AttachmentId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
}