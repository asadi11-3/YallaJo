using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Attachment.DeleteAttachment;

public sealed record DeleteAttachmentCommand(Guid AttachmentId) : ICommand;
