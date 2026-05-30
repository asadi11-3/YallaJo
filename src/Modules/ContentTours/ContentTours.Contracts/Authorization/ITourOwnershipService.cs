using YallaJo.SharedKernel.Application.Authorization;

namespace ContentTours.Contracts.Authorization;

/// <summary>
/// Cross-module ownership probe for Tour aggregates.
/// Implementations must use a read-only, no-tracking projection that ignores
/// soft-delete query filters so that callers can distinguish missing from deleted rows.
/// </summary>
public interface ITourOwnershipService
{
    Task<EntityOwnershipResolution> GetTourOwnershipAsync(Guid tourId, CancellationToken ct = default);
}
