namespace ContentCore.Presentation.Endpoints.Attachment.Models;

public sealed record SetPrimaryImageRequest(
    string EntityType,
    Guid EntityId,
    Guid AttachmentId);
