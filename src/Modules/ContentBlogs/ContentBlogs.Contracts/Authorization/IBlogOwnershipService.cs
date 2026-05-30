using YallaJo.SharedKernel.Application.Authorization;

namespace ContentBlogs.Contracts.Authorization;

/// <summary>
/// Cross-module ownership probe for Blog aggregates.
/// Implementations must use a read-only, no-tracking projection that ignores
/// soft-delete query filters so that callers can distinguish missing from deleted rows.
/// </summary>
public interface IBlogOwnershipService
{
    Task<EntityOwnershipResolution> GetBlogOwnershipAsync(Guid blogId, CancellationToken ct = default);
}
