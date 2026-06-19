namespace ContentCore.Contracts.Attachments;

/// <summary>
/// Best-effort cross-module cleanup of all attachments (DB rows + physical files)
/// belonging to a given entity. Intended for lifecycle hooks such as deleting a
/// review, where orphaned public images should be removed.
/// </summary>
public interface IEntityAttachmentCleanupService
{
    /// <summary>
    /// Removes all <c>Attachment</c> and <c>EntityImage</c> rows for the entity and
    /// best-effort deletes the backing physical files. Never throws for storage
    /// failures. Returns the number of attachment rows removed.
    /// </summary>
    Task<int> DeleteEntityAttachmentsAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);
}
