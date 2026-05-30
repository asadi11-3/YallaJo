using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record AttachmentUploadedDomainEvent(
    Guid AttachmentId,
    EntityType EntityType,
    Guid EntityId,
    AttachmentType AttachmentType,
    string Url) : DomainEventBase;