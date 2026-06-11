using YallaJo.SharedKernel.Application.Authorization;

namespace ContentBlogs.Contracts.Authorization;

/// <summary>
/// Cross-module ownership probe for CreatorProfile aggregates.
/// Implementations must use a read-only, no-tracking projection that ignores
/// soft-delete query filters so that callers can distinguish missing from deleted rows.
/// </summary>
public interface ICreatorOwnershipService
{
    Task<EntityOwnershipResolution> GetCreatorProfileOwnershipAsync(Guid profileId, CancellationToken ct = default);
}
