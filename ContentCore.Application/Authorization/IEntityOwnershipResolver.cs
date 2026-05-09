using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentCore.Application.Authorization;

/// <summary>
/// ContentCore-level fan-out for cross-module ownership probes. Maps an
/// <see cref="EntityType"/> to the appropriate per-module ownership service
/// (e.g. <c>IPlaceOwnershipService</c>, <c>ITourOwnershipService</c>) and
/// returns a uniform <see cref="EntityOwnershipResolution"/> result.
/// </summary>
public interface IEntityOwnershipResolver
{
    Task<EntityOwnershipResolution> ResolveAsync(
        EntityType entityType,
        Guid entityId,
        CancellationToken ct = default);
}
