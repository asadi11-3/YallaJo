using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Attachment.SetPrimaryImage;

public sealed record SetPrimaryImageCommand(
    EntityType EntityType,
    Guid EntityId,
    Guid AttachmentId) : ICommand;