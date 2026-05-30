namespace ContentBlogs.Application.Interfaces;

/// <summary>
/// Cross-module read interface to resolve entity IDs owned by a creator's provider account.
/// Implemented in the Accounts module (or a shared infrastructure adapter).
/// Used by disclosure validation (Wave 8 §6.3) to check whether a creator
/// is tagging entities they own without declaring sponsorship.
/// </summary>
public interface IProviderEntitiesReadClient
{
    /// <summary>
    /// Returns the set of entity IDs (tours, places, businesses) that belong to
    /// the approved provider associated with the given <paramref name="userId"/>.
    /// Returns an empty set if the user has no approved provider account.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetOwnedEntityIdsAsync(Guid userId, CancellationToken ct = default);
}
