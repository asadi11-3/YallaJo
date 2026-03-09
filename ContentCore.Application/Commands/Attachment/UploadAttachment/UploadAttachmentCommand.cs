using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Attachment.UploadAttachment;

public sealed record UploadAttachmentResult(Guid Id, string Url, long FileSize);

public sealed record UploadAttachmentCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    EntityType EntityType,
    Guid EntityId,
    AttachmentType Type,
    Guid UploadedByUserId,
    int? Width = null,
    int? Height = null,
    int? DurationSeconds = null,
    int SortOrder = 0) : ICommand<UploadAttachmentResult>;