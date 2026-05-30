using YallaJo.SharedKernel.Application.Authorization;

namespace ContentPlaces.Contracts.Places;

/// <summary>
/// Cross-module ownership probe for Place and Business aggregates.
/// Implementations must use a read-only, no-tracking projection that ignores
/// soft-delete query filters so that callers can distinguish missing from deleted rows.
/// </summary>
public interface IPlaceOwnershipService
{
    Task<EntityOwnershipResolution> GetPlaceOwnershipAsync(Guid placeId, CancellationToken ct = default);

    Task<EntityOwnershipResolution> GetBusinessOwnershipAsync(Guid businessId, CancellationToken ct = default);
}
