namespace ContentCore.Presentation.Endpoints.Attachment.Models;

public sealed record ReorderAttachmentsRequest(
    string EntityType,
    Guid EntityId,
    IReadOnlyList<Guid> OrderedAttachmentIds);
