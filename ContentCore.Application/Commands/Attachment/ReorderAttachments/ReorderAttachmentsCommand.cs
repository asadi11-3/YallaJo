using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Attachment.ReorderAttachments;

public sealed record ReorderAttachmentsCommand(
    EntityType EntityType,
    Guid EntityId,
    IReadOnlyList<Guid> OrderedAttachmentIds) : ICommand;
