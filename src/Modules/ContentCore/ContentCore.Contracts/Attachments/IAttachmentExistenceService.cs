namespace ContentCore.Contracts.Attachments;

public interface IAttachmentExistenceService
{
    Task<bool> HasEntityImageAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);
}
