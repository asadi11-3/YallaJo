namespace ContentCore.Contracts.Attachments;

public interface IPublicEntityImageReader
{
    Task<IReadOnlyList<EntityImageDto>> GetEntityImagesAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch-loads public entity images for multiple entities of the same type in a single query.
    /// Returns a dictionary keyed by entity id; entities with no images are absent from the dictionary.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<EntityImageDto>>> GetEntityImagesBatchAsync(
        string entityType,
        IReadOnlyCollection<Guid> entityIds,
        CancellationToken cancellationToken = default);
}
