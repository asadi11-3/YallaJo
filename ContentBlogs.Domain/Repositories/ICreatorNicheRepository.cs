using ContentBlogs.Domain.Entities.Creators;

namespace ContentBlogs.Domain.Repositories;

/// <summary>
/// Repository for <see cref="CreatorNiche"/> entities.
/// CreatorNiche is an AuditableEntity (admin-curated taxonomy) but not an IAggregateRoot,
/// so it uses a custom interface rather than <see cref="YallaJo.SharedKernel.Domain.Abstractions.Data.IRepository{TEntity,TKey}"/>.
/// </summary>
public interface ICreatorNicheRepository
{
    Task<CreatorNiche?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<CreatorNiche>> GetAllActiveAsync(
        CancellationToken cancellationToken = default);

    Task<bool> IsSlugTakenAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CreatorNiche niche,
        CancellationToken cancellationToken = default);

    void Update(CreatorNiche niche);

    /// <summary>
    /// Checks whether all the given niche IDs exist and are active.
    /// Used to validate application submissions.
    /// </summary>
    Task<bool> AllExistAndActiveAsync(
        IReadOnlyList<Guid> nicheIds,
        CancellationToken cancellationToken = default);
}
