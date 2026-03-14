using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record AttachmentDeletedDomainEvent(
    Guid AttachmentId,
    EntityType EntityType,
    Guid EntityId,
    AttachmentType AttachmentType,
    string Url) : DomainEventBase;