namespace ContentCore.Presentation.Endpoints.Attachment.Models;

public sealed record UploadAttachmentRequest(
    string EntityType,
    Guid EntityId,
    string AttachmentType,
    int? Width = null,
    int? Height = null,
    int? DurationSeconds = null,
    int SortOrder = 0);
