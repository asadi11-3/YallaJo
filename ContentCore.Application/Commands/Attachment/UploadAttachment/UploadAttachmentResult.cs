namespace ContentCore.Application.Commands.Attachment.UploadAttachment;

public sealed record UploadAttachmentResult(Guid Id, string Url, long FileSize);
