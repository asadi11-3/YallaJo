using ContentCore.Application.Queries.Attachment.Common;
using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Attachment.GetEntityAttachments;

// NOT cacheable (Patch 1B): this query is now ownership/admin-scoped. The cache key is
// caller-agnostic, so caching would let the first caller's authorized result be served to
// any later caller — re-introducing the IDOR the ownership guard closes. The handler must
// run on every request so the per-user authorization check always executes.
public sealed record GetEntityAttachmentsQuery(EntityType EntityType, Guid EntityId)
    : IQuery<IReadOnlyList<AttachmentDto>>;
