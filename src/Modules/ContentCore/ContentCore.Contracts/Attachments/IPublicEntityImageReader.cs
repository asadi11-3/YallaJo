namespace ContentCore.Contracts.Attachments;

public interface IPublicEntityImageReader
{
    Task<IReadOnlyList<EntityImageDto>> GetEntityImagesAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);
}
